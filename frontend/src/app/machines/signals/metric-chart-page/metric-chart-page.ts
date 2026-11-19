import { Component, computed, inject, linkedSignal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Popover } from 'primeng/popover';
import { Select } from 'primeng/select';
import { Tooltip } from 'primeng/tooltip';

import { isEmptyResult } from '@shared/grid/table/grid-display';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { LoadingSpinner } from '@shared/loading/loading-spinner/loading-spinner';
import { CLOCK } from '@shared/time/clock';
import { PlainDateTimeRangePicker } from '@shared/time/pickers/plain-date-time-range-picker/plain-date-time-range-picker';
import type { PlainDateTimeRange } from '@shared/time/plain-date-time-range';

import { ThemeStore } from '@shared/theme/theme.store';

import { MetricChart } from '@machines/signals/metric-chart/metric-chart';
import { limitsOf } from '@machines/signals/metric-chart/metric-chart.utils';
import { MetricChartPageStore } from '@machines/signals/metric-chart-page-store/metric-chart-page.store';
import {
  customBounds,
  RANGE_LABEL_KEYS,
  toPlainRange,
} from '@machines/signals/metric-chart-page-store/metric-chart-page.store.utils';
import {
  ChartRange,
  type MachineStateTimesResponse,
  type MetricBucket,
  type PhaseTimesResponse,
} from '@machines/signals/signal-episode';
import { TimeBreakdown } from '@machines/signals/time-breakdown/time-breakdown';
import { toPhaseRows, toStateRows } from '@machines/signals/time-breakdown/time-breakdown.utils';
import { STATE_LABEL_KEYS } from '@machines/list/machine-grid/machine-grid.utils';
import type { MachineState } from '@machines/machine';

@Component({
  imports: [
    ButtonDirective,
    EmptyState,
    FormsModule,
    LoadingSpinner,
    MetricChart,
    PlainDateTimeRangePicker,
    Popover,
    RouterLink,
    Select,
    TimeBreakdown,
    Tooltip,
    TranslatePipe,
  ],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4' },
  selector: 'hf-metric-chart-page',
  templateUrl: './metric-chart-page.html',
})
export class MetricChartPage {
  protected readonly store = inject(MetricChartPageStore);
  protected readonly theme = inject(ThemeStore);
  private readonly _clock = inject(CLOCK);
  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([
      'SIGNALS.TIME.UNKNOWN_MODE',
      'SIGNALS.TIME.NO_DATA',
      'SIGNALS.TIME.BEFORE_CREATION',
      'SIGNALS.TIME.DELETED',
      ...Object.values(STATE_LABEL_KEYS),
    ]),
    { initialValue: {} as Record<string, string> },
  );

  protected readonly rangeOptions = Object.values(ChartRange).map((value) => ({
    value,
    labelKey: RANGE_LABEL_KEYS[value],
  }));
  protected readonly metricOptions = computed(() => [...this.store.metrics()]);
  /** The buckets of the last finished load: a new load keeps them on the chart until its own buckets arrive. */
  protected readonly buckets = linkedSignal<readonly MetricBucket[] | null, readonly MetricBucket[]>({
    source: () => (this.store.history.hasValue() ? this.store.history.value().buckets : null),
    computation: (next, previous) => next ?? previous?.value ?? [],
  });
  protected readonly selectedPreset = computed(() => {
    const range = this.store.range();
    return range.kind === 'preset' ? range.preset : null;
  });
  /** The picked custom range. It holds one end until the user picks the other. */
  protected readonly pickedRange = linkedSignal<PlainDateTimeRange>(() =>
    toPlainRange(this.store.range(), this._clock.timeZoneId()),
  );
  protected readonly isZoomed = computed(() => this.store.zoomBase() !== null);

  /** The last finished reads: a new load keeps them on the page until its own answer arrives. */
  private readonly _phaseTimes = linkedSignal<PhaseTimesResponse | null, PhaseTimesResponse | null>({
    source: () => (this.store.phaseTimes.hasValue() ? this.store.phaseTimes.value() : null),
    computation: (next, previous) => next ?? previous?.value ?? null,
  });
  private readonly _stateTimes = linkedSignal<MachineStateTimesResponse | null, MachineStateTimesResponse | null>({
    source: () => (this.store.stateTimes.hasValue() ? this.store.stateTimes.value() : null),
    computation: (next, previous) => next ?? previous?.value ?? null,
  });
  protected readonly phaseRows = computed(() => {
    const times = this._phaseTimes();
    return times === null
      ? []
      : toPhaseRows(times, {
          unknownMode: this._text('SIGNALS.TIME.UNKNOWN_MODE'),
          noData: this._text('SIGNALS.TIME.NO_DATA'),
          deleted: this._text('SIGNALS.TIME.DELETED'),
        });
  });
  protected readonly stateRows = computed(() => {
    const times = this._stateTimes();
    return times === null
      ? []
      : toStateRows(times, {
          stateOf: (value) => this._text(STATE_LABEL_KEYS[value as MachineState] ?? value),
          beforeCreation: this._text('SIGNALS.TIME.BEFORE_CREATION'),
        });
  });
  protected readonly hasNoPhases = computed(() => this._phaseTimes()?.phases.length === 0);
  protected readonly showTimesSpinner = delayedLoading(
    computed(() => this.store.phaseTimes.isLoading() || this.store.stateTimes.isLoading()),
  );
  protected readonly limits = computed(() => limitsOf(this.store.selectedMetric()));
  protected readonly unit = computed(() => this.store.selectedMetric()?.unit.abbreviation ?? '');
  protected readonly showSpinner = delayedLoading(
    computed(() => this.store.definitions.isLoading() || this.store.history.isLoading()),
  );
  protected readonly hasNoMetric = computed(
    () => this.store.definitions.hasValue() && this.store.selectedMetric() === null,
  );
  protected readonly showEmpty = computed(
    () =>
      this.hasNoMetric() ||
      isEmptyResult({
        rows: this.buckets(),
        isLoading: this.store.history.isLoading(),
        hasLoaded: this.store.history.hasValue(),
      }),
  );

  protected onRangePicked(range: PlainDateTimeRange): void {
    this.pickedRange.set(range);
    const bounds = customBounds(range, this._clock.timeZoneId());
    if (bounds !== null) {
      this.store.selectCustomRange(bounds.from, bounds.to);
    }
  }

  private _text(key: string): string {
    return this._labels()[key] ?? key;
  }
}
