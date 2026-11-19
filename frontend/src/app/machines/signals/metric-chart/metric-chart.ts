import { DOCUMENT } from '@angular/common';
import { Component, computed, DestroyRef, ElementRef, inject, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import type { Chart, ChartOptions, ChartTypeRegistry, LegendItem, TooltipItem } from 'chart.js';
import zoomPlugin from 'chartjs-plugin-zoom';
import { ChartModule } from 'primeng/chart';

import { cssColor } from '@shared/theme/css-color';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import {
  BAND_ALPHA,
  type ChartColors,
  type ChartDataset,
  type ChartLimits,
  type ChartView,
  DEFAULT_HIDDEN_GROUPS,
  EPISODE_AXIS_ID,
  hasTooltipLines,
  isLegendEntry,
  isTooltipEntry,
  scaleRange,
  tickLabel,
  TIME_FORMAT,
  timeTicks,
  toChartData,
  toggledGroups,
  tooltipLabel,
  tooltipSwatch,
  withAlpha,
  zoomLimits,
} from '@machines/signals/metric-chart/metric-chart.utils';
import { type MetricBucket, type SignalEpisode, SignalSeverity } from '@machines/signals/signal-episode';

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
  /** The times that the buckets cover: the axis shows exactly this view, also after a zoom. */
  readonly view = input.required<ChartView>();
  /** The times that a zoom or a pan may not leave: the view before the first zoom. */
  readonly bounds = input.required<ChartView>();
  /** The buckets that the history asked for, not the ones that came back: a quiet range returns fewer. */
  readonly bucketCount = input.required<number>();
  readonly unit = input('');
  /** The theme of the page. A change draws the chart again with the colors of the new theme. */
  readonly isDark = input(false);

  /** The chart width in CSS pixels, after each size change. */
  readonly widthChange = output<number>();
  /** The range that a zoom or a pan shows. */
  readonly zoomed = output<ZoomedRange>();

  protected readonly plugins = [zoomPlugin];

  private readonly _host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly _document = inject(DOCUMENT);
  private readonly _clock = inject(CLOCK);
  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(this._translate.stream([...LABEL_KEYS]), {
    initialValue: {} as Record<string, string>,
  });
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
      viewEnd: Date.parse(this.view().to),
      labels: {
        avg: text('SIGNALS.CHART.AVG'),
        range: text('SIGNALS.CHART.RANGE'),
        warningLimits: text('SIGNALS.CHART.WARNING_LIMITS'),
        alarmLimits: text('SIGNALS.CHART.ALARM_LIMITS'),
        signals: text('SIGNALS.CHART.SIGNALS'),
      },
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
    const zone = this._clock.timeZoneId();
    const view = this.view();
    const limits = zoomLimits(this.bounds(), this.bucketCount());
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
            labelColor: (item: TooltipItem<keyof ChartTypeRegistry>) =>
              tooltipSwatch(item.dataset as unknown as ChartDataset),
            title: (items: TooltipItem<keyof ChartTypeRegistry>[]) =>
              items.length === 0 ? '' : tickLabel(Number(items[0].parsed.x), zone, 0),
            label: (item: TooltipItem<keyof ChartTypeRegistry>) =>
              tooltipLabel(
                item.chart.data.datasets as unknown as ChartDataset[],
                item.datasetIndex,
                item.dataIndex,
                (value) => new Intl.NumberFormat(item.chart.options.locale).format(value),
              ),
          },
          filter: (item: TooltipItem<keyof ChartTypeRegistry>) =>
            isTooltipEntry(item.dataset as unknown as ChartDataset) &&
            hasTooltipLines(item.dataset as unknown as ChartDataset, item.dataIndex),
        },
        zoom: {
          limits: { x: { min: limits.min, max: limits.max, minRange: limits.minRange } },
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
        x: {
          type: 'linear' as const,
          min: Date.parse(view.from),
          max: Date.parse(view.to),
          afterBuildTicks: (scale) => {
            scale.ticks = timeTicks(scale.min, scale.max, zone, MAX_TICKS).map((value) => ({ value }));
          },
          ticks: {
            color: muted,
            maxTicksLimit: MAX_TICKS,
            callback: (value, _index, ticks) =>
              tickLabel(Number(value), zone, Number(ticks.at(-1)?.value ?? 0) - Number(ticks[0]?.value ?? 0)),
          },
          grid: { color: grid },
        },
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
  }

  /** The exact times of the axis at the end of a gesture. The new buckets of this range fill the same view. */
  private _emitZoom(chart: Chart): void {
    const scale = chart.scales['x'];
    this.zoomed.emit(scaleRange(Number(scale.min), Number(scale.max)));
  }
}
