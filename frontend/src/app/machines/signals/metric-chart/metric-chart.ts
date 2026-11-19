import { DOCUMENT } from '@angular/common';
import { Component, computed, DestroyRef, effect, ElementRef, inject, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import type { Chart, ChartOptions, ChartTypeRegistry, LegendItem, TooltipItem } from 'chart.js';
import zoomPlugin from 'chartjs-plugin-zoom';
import { ChartModule } from 'primeng/chart';

import { cssColor } from '@shared/theme/css-color';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import { zoomedBounds } from '@machines/signals/metric-chart-page-store/metric-chart-page.store.utils';
import {
  type ChartColors,
  type ChartDataset,
  type ChartLimits,
  DEFAULT_HIDDEN_GROUPS,
  EPISODE_AXIS_ID,
  isLegendEntry,
  toChartData,
  toggledGroups,
  withAlpha,
} from '@machines/signals/metric-chart/metric-chart.utils';
import { type MetricBucket, type SignalEpisode, SignalSeverity } from '@machines/signals/signal-episode';

const BAND_ALPHA = 0.18;
const MAX_TICKS = 8;
const PAN_MODIFIER_KEY = 'shift';
const LABEL_KEYS = [
  'SIGNALS.CHART.AVG',
  'SIGNALS.CHART.RANGE',
  'SIGNALS.CHART.WARNING_LIMITS',
  'SIGNALS.CHART.ALARM_LIMITS',
  'SIGNALS.CHART.SIGNALS',
  'SIGNALS.CHART.EPISODE_OPEN',
  'MAINTENANCE.SEVERITIES.WARNING',
  'MAINTENANCE.SEVERITIES.ALARM',
] as const;
const TIME_FORMAT: Intl.DateTimeFormatOptions = {
  day: '2-digit',
  month: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
};
const EPISODE_SEPARATOR = ' · ';
const RANGE_SEPARATOR = ' – ';

/** The from-to that a zoom or a pan of the chart shows. The times are ISO instants. */
export interface ZoomedRange {
  readonly from: string;
  readonly to: string;
}

/**
 * Draws the history of one metric with PrimeNG `p-chart`. It reads no store: the page passes the data, takes the width,
 * and takes the range that a zoom or a pan shows. Only this chart registers the zoom plugin.
 */
@Component({
  host: { class: 'relative block min-h-0 min-w-0 flex-1' },
  imports: [ChartModule],
  selector: 'hf-metric-chart',
  template: '<p-chart type="line" height="100%" [data]="data()" [options]="options()" [plugins]="plugins" />',
})
export class MetricChart {
  readonly buckets = input.required<readonly MetricBucket[]>();
  readonly limits = input.required<ChartLimits>();
  readonly episodes = input.required<readonly SignalEpisode[]>();
  readonly unit = input('');
  /** The theme of the page. A change draws the chart again with the colors of the new theme. */
  readonly isDark = input(false);

  /** The chart width in CSS pixels, after each size change. */
  readonly widthChange = output<number>();
  /** The range that a zoom or a pan shows. */
  readonly zoomed = output<ZoomedRange>();

  /** The zoom plugin, and a small plugin that keeps the chart instance for the zoom reset. */
  protected readonly plugins = [
    zoomPlugin,
    {
      id: 'hfChartHandle',
      afterInit: (chart: Chart): void => {
        this._chart = chart;
      },
      afterDestroy: (): void => {
        this._chart = null;
      },
    },
  ];

  private readonly _host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly _document = inject(DOCUMENT);
  private readonly _clock = inject(CLOCK);
  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(this._translate.stream([...LABEL_KEYS]), {
    initialValue: {} as Record<string, string>,
  });
  private _chart: Chart | null = null;
  private _isResettingZoom = false;
  private readonly _hiddenGroups = signal<ReadonlySet<string>>(DEFAULT_HIDDEN_GROUPS);

  protected readonly data = computed(() => {
    const labels = this._labels();
    const text = (key: (typeof LABEL_KEYS)[number]): string => labels[key] ?? key;
    const zone = this._clock.timeZoneId();
    const formatTime = (iso: string): string =>
      Temporal.Instant.from(iso).toZonedDateTimeISO(zone).toLocaleString(undefined, TIME_FORMAT);
    const severityKeys = {
      [SignalSeverity.Warning]: 'MAINTENANCE.SEVERITIES.WARNING',
      [SignalSeverity.Alarm]: 'MAINTENANCE.SEVERITIES.ALARM',
    } as const;
    return toChartData({
      buckets: this.buckets(),
      limits: this.limits(),
      episodes: this.episodes(),
      colors: this._colors(),
      hiddenGroups: this._hiddenGroups(),
      labels: {
        avg: text('SIGNALS.CHART.AVG'),
        range: text('SIGNALS.CHART.RANGE'),
        warningLimits: text('SIGNALS.CHART.WARNING_LIMITS'),
        alarmLimits: text('SIGNALS.CHART.ALARM_LIMITS'),
        signals: text('SIGNALS.CHART.SIGNALS'),
      },
      formatTime,
      episodeText: (episode) =>
        [
          episode.rule,
          text(severityKeys[episode.severity]),
          episode.metricKey ?? '',
          formatTime(episode.openedAt) +
            RANGE_SEPARATOR +
            (episode.resolvedAt === null ? text('SIGNALS.CHART.EPISODE_OPEN') : formatTime(episode.resolvedAt)),
        ]
          .filter((part) => part !== '')
          .join(EPISODE_SEPARATOR),
    });
  });

  protected readonly options = computed((): ChartOptions => {
    this.isDark();
    const muted = cssColor(this._document, '--p-text-muted-color');
    const grid = cssColor(this._document, '--p-content-border-color');
    return {
      responsive: true,
      maintainAspectRatio: false,
      animation: false,
      interaction: { mode: 'index' as const, intersect: false },
      plugins: {
        legend: {
          position: 'bottom' as const,
          labels: {
            color: muted,
            filter: (item: LegendItem, data) => isLegendEntry(data.datasets[item.datasetIndex ?? 0] as ChartDataset),
          },
          onClick: (_event, item) => {
            const group = (this.data().datasets[item.datasetIndex ?? 0] as ChartDataset | undefined)?.hfGroup;
            if (group !== undefined) {
              this._hiddenGroups.update((hidden) => toggledGroups(hidden, group));
            }
          },
        },
        tooltip: {
          callbacks: {
            label: (item: TooltipItem<keyof ChartTypeRegistry>) =>
              (item.dataset as unknown as ChartDataset).hfTooltip ??
              `${item.dataset.label ?? ''}: ${item.formattedValue}`,
          },
        },
        zoom: {
          zoom: {
            wheel: { enabled: true },
            drag: { enabled: true },
            mode: 'x',
            onZoomComplete: ({ chart }) => this._emitZoom(chart),
          },
          pan: {
            enabled: true,
            mode: 'x',
            modifierKey: PAN_MODIFIER_KEY,
            onPanComplete: ({ chart }) => this._emitZoom(chart),
          },
        },
      },
      scales: {
        x: { ticks: { color: muted, maxTicksLimit: MAX_TICKS }, grid: { color: grid } },
        y: {
          title: { display: this.unit() !== '', text: this.unit(), color: muted },
          ticks: { color: muted },
          grid: { color: grid },
        },
        [EPISODE_AXIS_ID]: { display: false, min: 0, max: 1, grid: { display: false } },
      },
    };
  });

  /** Read from the CSS tokens again with each theme change. */
  private readonly _colors = computed((): ChartColors => {
    this.isDark();
    const avg = cssColor(this._document, '--p-primary-color');
    return {
      avg,
      band: withAlpha(avg, BAND_ALPHA),
      warning: cssColor(this._document, '--p-orange-500'),
      alarm: cssColor(this._document, '--p-red-500'),
      muted: cssColor(this._document, '--p-text-muted-color'),
    };
  });

  constructor() {
    const observer = new ResizeObserver((entries) => {
      const width = entries[0]?.contentRect.width ?? 0;
      if (width > 0) {
        this.widthChange.emit(width);
      }
    });
    observer.observe(this._host.nativeElement);
    inject(DestroyRef).onDestroy(() => observer.disconnect());
    effect(() => {
      this.buckets();
      this._resetZoomState();
    });
  }

  /**
   * New buckets show the range of the zoom, so the zoom of the plugin ends: the axis shows all buckets again. The reset
   * runs after the chart took the new data.
   */
  private _resetZoomState(): void {
    queueMicrotask(() => {
      this._isResettingZoom = true;
      try {
        this._chart?.resetZoom('none');
      } finally {
        this._isResettingZoom = false;
      }
    });
  }

  /** The plugin calls `onZoomComplete` for our own reset too. That call is no zoom of the user, so it emits nothing. */
  private _emitZoom(chart: Chart): void {
    if (this._isResettingZoom) {
      return;
    }
    const scale = chart.scales['x'];
    const bounds = zoomedBounds(this.buckets(), Number(scale.min), Number(scale.max));
    if (bounds !== null) {
      this.zoomed.emit(bounds);
    }
  }
}
