import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { debounceTime, EMPTY, interval, map, type Observable, pipe, switchMap, tap } from 'rxjs';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';
import { skipErrorToast } from '@shared/http/http-error-key';
import { withRefreshCooldown } from '@shared/refresh/with-refresh-cooldown';
import { CLOCK } from '@shared/time/clock';

import type { Machine } from '@machines/machine';
import { MachinesApi } from '@machines/machines-api';
import type { MetricDefinition } from '@machines/metrics/metric-definition';
import { MetricDefinitionsApi } from '@machines/metrics/metric-definitions-api';
import {
  bucketCountForWidth,
  chartableMetrics,
  DEFAULT_RANGE,
  type LoadedHistory,
  METRIC_CHART_RELOAD_INTERVAL_MS,
  RANGE_DEBOUNCE_MS,
  type RangeSelection,
  selectionBounds,
} from '@machines/signals/metric-chart-page-store/metric-chart-page.store.utils';
import type { ChartRange } from '@machines/signals/signal-episode';
import { SignalsApi } from '@machines/signals/signals-api';

/** The pause after the last width change before the chart asks for the history again. */
const WIDTH_DEBOUNCE_MS = 250;

interface ScopeState {
  machineId: string | null;
  machine: Machine | null;
  metricId: string | null;
  /** The metric key of a link (the case page): used while the user has chosen no metric. */
  preferredMetricKey: string | null;
  /** The range that the controls show. */
  range: RangeSelection;
  /** The range that the loads use: it follows `range` after the pause of `RANGE_DEBOUNCE_MS`. */
  loadedRange: RangeSelection;
  /** The range before the first zoom, for **Reset zoom**; `null` while the chart is not zoomed. */
  zoomBase: RangeSelection | null;
  /** The times of the view before the first zoom: the limits of the zoom and the pan; `null` while the chart is not zoomed. */
  zoomBounds: { from: string; to: string } | null;
  /** The range of the time in phases and states: it follows `loadedRange`, but a zoom of the chart does not move it. */
  statsRange: RangeSelection;
  bucketCount: number | null;
  refreshCount: number;
}

export const MetricChartPageStore = signalStore(
  withState<ScopeState>({
    machineId: null,
    machine: null,
    metricId: null,
    preferredMetricKey: null,
    range: DEFAULT_RANGE,
    loadedRange: DEFAULT_RANGE,
    zoomBase: null,
    zoomBounds: null,
    statsRange: DEFAULT_RANGE,
    bucketCount: null,
    refreshCount: 0,
  }),
  withRefreshCooldown(),
  withProps(() => ({
    _clock: inject(CLOCK),
    _api: inject(SignalsApi),
    _machinesApi: inject(MachinesApi),
    _metricsApi: inject(MetricDefinitionsApi),
  })),
  withProps((store) => ({
    definitions: rxResource({
      params: () => store.machineId() ?? undefined,
      stream: ({ params }) => store._metricsApi.getDefinitions(params, skipErrorToast()),
    }),
  })),
  withComputed((store) => ({
    metrics: computed((): readonly MetricDefinition[] =>
      store.definitions.hasValue() ? chartableMetrics(store.definitions.value().definitions) : [],
    ),
  })),
  withComputed((store) => ({
    selectedMetric: computed((): MetricDefinition | null => {
      const metrics = store.metrics();
      return (
        metrics.find((metric) => metric.id === store.metricId()) ??
        metrics.find((metric) => metric.key === store.preferredMetricKey()) ??
        metrics[0] ??
        null
      );
    }),
  })),
  withProps((store) => ({
    history: rxResource({
      params: () => {
        const machineId = store.machineId();
        const metric = store.selectedMetric();
        const bucketCount = store.bucketCount();
        store.refreshCount();
        return machineId === null || metric === null || bucketCount === null
          ? undefined
          : { machineId, metricDefinitionId: metric.id, range: store.loadedRange(), bucketCount };
      },
      stream: ({ params }): Observable<LoadedHistory> => {
        const bounds = selectionBounds(store._clock.now(), params.range);
        return store._api
          .getHistory(params.machineId, {
            metricDefinitionId: params.metricDefinitionId,
            ...bounds,
            bucketCount: params.bucketCount,
          })
          .pipe(map((history) => ({ ...history, ...bounds })));
      },
    }),
    phaseTimes: rxResource({
      params: () => {
        const machineId = store.machineId();
        store.refreshCount();
        return machineId === null ? undefined : { machineId, range: store.statsRange() };
      },
      stream: ({ params }) =>
        store._api.getPhaseTimes(params.machineId, selectionBounds(store._clock.now(), params.range)),
    }),
    stateTimes: rxResource({
      params: () => {
        const machineId = store.machineId();
        store.refreshCount();
        return machineId === null ? undefined : { machineId, range: store.statsRange() };
      },
      stream: ({ params }) =>
        store._machinesApi.getStateTimes(params.machineId, selectionBounds(store._clock.now(), params.range)),
    }),
    episodes: rxResource({
      params: () => {
        const machineId = store.machineId();
        store.refreshCount();
        return machineId === null ? undefined : { machineId, range: store.loadedRange() };
      },
      stream: ({ params }) =>
        store._api.getEpisodes(params.machineId, selectionBounds(store._clock.now(), params.range)),
    }),
  })),
  withComputed((store) => ({
    metricEpisodes: computed(() => {
      const metric = store.selectedMetric();
      return store.episodes.hasValue() && metric !== null
        ? store.episodes.value().items.filter((episode) => episode.metricDefinitionId === metric.id)
        : [];
    }),
  })),
  withMethods((store) => {
    const applyWidth = rxMethod<number>(
      pipe(
        debounceTime(WIDTH_DEBOUNCE_MS),
        tap((width) => patchState(store, { bucketCount: bucketCountForWidth(width) })),
      ),
    );
    const applyRange = rxMethod<RangeSelection>(
      pipe(
        debounceTime(RANGE_DEBOUNCE_MS),
        tap((loadedRange) =>
          patchState(store, { loadedRange, ...(store.zoomBase() === null ? { statsRange: loadedRange } : {}) }),
        ),
      ),
    );
    const showRange = (
      range: RangeSelection,
      zoomBase: RangeSelection | null,
      zoomBounds: { from: string; to: string } | null,
    ): void => {
      patchState(store, { range, zoomBase, zoomBounds });
      applyRange(range);
    };
    /** The times that the chart shows now: the last finished history, or else the range of the selection. */
    const shownBounds = (): { from: string; to: string } => {
      const history = store.history.hasValue() ? store.history.value() : null;
      return history === null
        ? selectionBounds(store._clock.now(), store.loadedRange())
        : { from: history.from, to: history.to };
    };
    return {
      /**
       * Starts a visit to the signals of a machine: loads the machine, then resets the selection. The state changes only
       * after a successful load. The load skips the error toast, so the caller decides what a failure means.
       */
      openMachine(machineId: string): Observable<Machine> {
        return store._machinesApi.get(machineId, skipErrorToast()).pipe(
          tap((machine) =>
            patchState(store, {
              machineId,
              machine,
              metricId: null,
              preferredMetricKey: null,
              range: DEFAULT_RANGE,
              loadedRange: DEFAULT_RANGE,
              statsRange: DEFAULT_RANGE,
              zoomBase: null,
              zoomBounds: null,
            }),
          ),
        );
      },
      /** Shows the metric with the key after the definitions load. An unknown key keeps the default metric. */
      preferMetricKey(metricKey: string): void {
        patchState(store, { preferredMetricKey: metricKey });
      },
      selectMetric(metricId: string): void {
        patchState(store, { metricId });
      },
      selectRange(preset: ChartRange): void {
        showRange({ kind: 'preset', preset }, null, null);
      },
      /** Shows a custom from-to range (ISO instants). The history loads after the pause. */
      selectCustomRange(from: string, to: string): void {
        showRange({ kind: 'custom', from, to }, null, null);
      },
      /** A zoom or a pan of the chart: shows the from-to of the view and keeps the range before the first zoom. */
      zoomTo(from: string, to: string): void {
        showRange({ kind: 'custom', from, to }, store.zoomBase() ?? store.range(), store.zoomBounds() ?? shownBounds());
      },
      /** Returns to the range before the zoom. */
      resetZoom(): void {
        const base = store.zoomBase();
        if (base !== null) {
          showRange(base, null, null);
        }
      },
      /** Takes the chart width in CSS pixels. The first width counts at once; later ones follow after a pause. */
      setWidth(width: number): void {
        if (store.bucketCount() === null) {
          patchState(store, { bucketCount: bucketCountForWidth(width) });
          return;
        }
        applyWidth(width);
      },
      /** Reloads the history and the episodes, once per cooldown. */
      refresh(): void {
        if (store.isRefreshCoolingDown()) {
          return;
        }
        patchState(store, { refreshCount: store.refreshCount() + 1 });
        store.startRefreshCooldown();
      },
    };
  }),
  withComputed((store) => ({
    /** True while the range is a preset that ends now. A custom range, a zoom, and a pan all make it custom. */
    isLiveRange: computed(() => store.machineId() !== null && store.range().kind === 'preset'),
  })),
  withHooks({
    onInit(store) {
      const reloadWhileLive = rxMethod<boolean>(
        pipe(
          switchMap((isLive) =>
            isLive
              ? interval(METRIC_CHART_RELOAD_INTERVAL_MS).pipe(
                  tap(() => {
                    store.history.reload();
                  }),
                )
              : EMPTY,
          ),
        ),
      );
      reloadWhileLive(store.isLiveRange);
    },
  }),
  withStoreDevtools('metric-chart-page'),
);
