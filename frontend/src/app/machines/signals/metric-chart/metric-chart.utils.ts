import { Temporal } from '@shared/time/temporal';

import { type MetricBucket, type SignalEpisode, SignalSeverity } from '@machines/signals/signal-episode';

export interface ChartLimits {
  readonly warningLow: number | null;
  readonly warningHigh: number | null;
  readonly alarmLow: number | null;
  readonly alarmHigh: number | null;
}

export interface ChartColors {
  readonly avg: string;
  readonly band: string;
  readonly warning: string;
  readonly alarm: string;
  /** The color of the legend entry of the signals. */
  readonly muted: string;
}

/** One point of a dataset: `x` is the epoch milliseconds of its bucket start, `y` is `null` where the line has a gap. */
export interface ChartPoint {
  readonly x: number;
  readonly y: number | null;
}

/**
 * One dataset of the chart. `hfGroup` names the legend entry that the dataset belongs to: a click on the entry shows or
 * hides the whole group. `hfLegend: false` keeps the dataset out of the legend, because its group has another dataset
 * that carries the entry. `hfSignals` marks the dataset of a signal area and lists its signals for the tooltip.
 * `hfTooltipHidden` keeps the dataset out of
 * the tooltip: the upper edge of the range band, whose value the line of the lower edge shows.
 */
export interface ChartDataset {
  readonly type: 'line';
  readonly label: string;
  readonly data: ChartPoint[];
  readonly hfGroup: string;
  readonly hfLegend?: false;
  readonly hfTooltipHidden?: true;
  readonly hfSignals?: readonly AreaSignal[];
  readonly [option: string]: unknown;
}

/** The color box of one tooltip line. */
export interface TooltipSwatch {
  readonly borderColor: string;
  readonly backgroundColor: string;
  readonly borderWidth: number;
}

/**
 * A solid box in the color that the dataset draws with: its line color, or its fill color without alpha when it draws no
 * line (Chart.js paints the box on white, so a translucent fill would show almost white). Chart.js
 * otherwise frames the box with the line width of the dataset, so the 6 px signal strip gave a bold frame.
 */
export function tooltipSwatch(dataset: Readonly<Record<string, unknown>>): TooltipSwatch {
  const width = dataset['borderWidth'];
  const drawsLine = typeof width === 'number' && width > 0;
  const color = String((drawsLine ? dataset['borderColor'] : dataset['backgroundColor']) ?? 'transparent');
  const solid = drawsLine ? color : withoutAlpha(color);
  return { borderColor: solid, backgroundColor: solid, borderWidth: 0 };
}

const VALUE_RANGE_SEPARATOR = ' – ';

/** Whether the tooltip shows a line for the dataset. */
export function isTooltipEntry(dataset: Pick<ChartDataset, 'hfTooltipHidden'>): boolean {
  return dataset.hfTooltipHidden !== true;
}

/**
 * Whether the tooltip has a line for the dataset at the bucket: a signal area has one only where a signal covers the
 * bucket. The closing point of an area, on the next bucket, has none.
 */
export function hasTooltipLines(dataset: Pick<ChartDataset, 'hfSignals'>, dataIndex: number): boolean {
  return dataset.hfSignals === undefined || dataset.hfSignals.some((s) => s.first <= dataIndex && dataIndex <= s.last);
}

/**
 * The tooltip lines of one dataset at one bucket: one line for each signal of an area that covers the bucket, the
 * minimum and the maximum of the range band in one line, or the label and the value.
 */
export function tooltipLabel(
  datasets: readonly ChartDataset[],
  datasetIndex: number,
  dataIndex: number,
  format: (value: number) => string,
): string | string[] {
  const dataset = datasets[datasetIndex];
  if (dataset.hfSignals !== undefined) {
    return dataset.hfSignals
      .filter((signal) => signal.first <= dataIndex && dataIndex <= signal.last)
      .map((signal) => signal.text);
  }
  const value = dataset.data[dataIndex]?.y ?? null;
  const upper =
    dataset.hfGroup === ChartGroup.Range
      ? (datasets.find((candidate) => candidate.hfGroup === ChartGroup.Range && !isTooltipEntry(candidate))?.data[
          dataIndex
        ]?.y ?? null)
      : null;
  if (value !== null && upper !== null) {
    return `${dataset.label}: ${format(value)}${VALUE_RANGE_SEPARATOR}${format(upper)}`;
  }
  return `${dataset.label}: ${value === null ? '' : format(value)}`;
}

export interface ChartDataModel {
  datasets: ChartDataset[];
}

export interface ChartInput {
  readonly buckets: readonly MetricBucket[];
  readonly limits: ChartLimits;
  readonly episodes: readonly SignalEpisode[];
  readonly colors: ChartColors;
  readonly labels: ChartLabels;
  /** The tooltip text of an episode: its rule, severity, metric, and local start and end. */
  readonly episodeText: (episode: SignalEpisode) => string;
  /** The legend groups that the user hid. The chart shows the warning limits hidden at first (`DEFAULT_HIDDEN_GROUPS`). */
  readonly hiddenGroups: ReadonlySet<string>;
  /** The end of the view in epoch milliseconds: an open signal runs to it. */
  readonly viewEnd: number;
}

/** The 5 legend entries of the chart. */
export interface ChartLabels {
  readonly avg: string;
  readonly range: string;
  readonly warningLimits: string;
  readonly alarmLimits: string;
  readonly signals: string;
}

/** The ids of the legend groups. */
export enum ChartGroup {
  Avg = 'avg',
  Range = 'range',
  WarningLimits = 'warningLimits',
  AlarmLimits = 'alarmLimits',
  Signals = 'signals',
}

function epochMilliseconds(iso: string): number {
  return Temporal.Instant.from(iso).epochMilliseconds;
}

/** The smallest view of a zoom, in buckets (Artur, 2026-11-19: "2 buckets ok"). */
const MIN_ZOOM_BUCKETS = 2;

/** The groups that start hidden: the warning limits show after a click on their legend entry. */
export const DEFAULT_HIDDEN_GROUPS: ReadonlySet<string> = new Set([ChartGroup.WarningLimits]);

/** The id of the hidden axis of the signal areas, from 0 to 1: an area reaches the full height. */
export const EPISODE_AXIS_ID = 'episodes';

/** The alpha of the tint of the range band and of the signal areas. */
export const BAND_ALPHA = 0.18;
const LIMIT_DASH: readonly number[] = [6, 4];
const NO_LIMITS: ChartLimits = { warningLow: null, warningHigh: null, alarmLow: null, alarmHigh: null };

/** The limits of a metric definition. A definition that does not carry limits gives no limit line. */
export function limitsOf(metric: Partial<ChartLimits> | null): ChartLimits {
  return metric === null
    ? NO_LIMITS
    : {
        warningLow: metric.warningLow ?? null,
        warningHigh: metric.warningHigh ?? null,
        alarmLow: metric.alarmLow ?? null,
        alarmHigh: metric.alarmHigh ?? null,
      };
}

/**
 * The first and the last bucket that the episode overlaps, or `null` when it overlaps none. A bucket runs from its start
 * to the start of the next one, and the last bucket has no end.
 */
export function episodeBucketRange(
  buckets: readonly Pick<MetricBucket, 'start'>[],
  episode: Pick<SignalEpisode, 'openedAt' | 'resolvedAt'>,
): { readonly first: number; readonly last: number } | null {
  const starts = buckets.map((bucket) => epochMilliseconds(bucket.start));
  const opened = epochMilliseconds(episode.openedAt);
  const resolved = episode.resolvedAt === null ? Number.POSITIVE_INFINITY : epochMilliseconds(episode.resolvedAt);
  const overlapping = starts
    .map((start, index) => ({
      start,
      end: index + 1 < starts.length ? starts[index + 1] : Number.POSITIVE_INFINITY,
      index,
    }))
    .filter((bucket) => opened < bucket.end && resolved > bucket.start)
    .map((bucket) => bucket.index);
  return overlapping.length === 0 ? null : { first: overlapping[0], last: overlapping[overlapping.length - 1] };
}

function limitLine(
  group: ChartGroup,
  label: string,
  value: number,
  color: string,
  xs: readonly number[],
  isFirst: boolean,
): ChartDataset {
  return {
    type: 'line',
    label,
    data: xs.map((x) => ({ x, y: value })),
    hfGroup: group,
    ...(isFirst ? {} : { hfLegend: false as const }),
    borderColor: color,
    borderDash: [...LIMIT_DASH],
    borderWidth: 1,
    pointRadius: 0,
    fill: false,
    order: 2,
  };
}

function limitGroup(
  group: ChartGroup,
  label: string,
  values: readonly (number | null)[],
  color: string,
  xs: readonly number[],
): ChartDataset[] {
  return values
    .filter((value): value is number => value !== null)
    .map((value, index) => limitLine(group, label, value, color, xs, index === 0));
}

/**
 * The points of an area: 1 on the buckets that it overlaps, and 1 on the start of the next bucket (on the end of the
 * view after the last bucket), so an area of one bucket has a width and fills its whole last bucket.
 */
function areaPoints(
  range: { readonly first: number; readonly last: number },
  xs: readonly number[],
  viewEnd: number,
): ChartPoint[] {
  const points: ChartPoint[] = xs.map((x, index) => ({
    x,
    y: index >= range.first && index <= range.last + 1 ? 1 : null,
  }));
  return range.last + 1 < xs.length ? points : [...points, { x: Math.max(viewEnd, xs[range.last]), y: 1 }];
}

function areaDataset(
  area: SignalArea,
  range: { readonly first: number; readonly last: number },
  xs: readonly number[],
  input: ChartInput,
): ChartDataset {
  const color = area.severity === SignalSeverity.Alarm ? input.colors.alarm : input.colors.warning;
  const hfSignals = area.episodes.flatMap((episode) => {
    const signalRange = episodeBucketRange(input.buckets, episode);
    return signalRange === null ? [] : [{ ...signalRange, text: input.episodeText(episode) }];
  });
  return {
    type: 'line',
    label: input.labels.signals,
    data: areaPoints(range, xs, input.viewEnd),
    hfGroup: ChartGroup.Signals,
    hfLegend: false,
    hfSignals,
    yAxisID: EPISODE_AXIS_ID,
    backgroundColor: withAlpha(color, BAND_ALPHA),
    borderWidth: 0,
    pointRadius: 0,
    fill: 'origin',
    order: 4,
  };
}

/**
 * Maps the history to the datasets of the chart, in 5 legend entries: the average line, the range band between the
 * minimum and the maximum (no lines of its own), the warning limits (hidden at first), the alarm limits, and the signals
 * (a thin strip with one segment per episode, in the color of its severity).
 */
export function toChartData(input: ChartInput): ChartDataModel {
  const { buckets, limits, colors, labels } = input;
  const xs = buckets.map((bucket) => epochMilliseconds(bucket.start));
  const areas = signalAreas(input.episodes, input.viewEnd).flatMap((area) => {
    const range = episodeBucketRange(buckets, {
      openedAt: Temporal.Instant.fromEpochMilliseconds(area.from).toString(),
      resolvedAt: Temporal.Instant.fromEpochMilliseconds(area.to).toString(),
    });
    return range === null ? [] : [areaDataset(area, range, xs, input)];
  });

  const datasets: ChartDataset[] = [
    {
      type: 'line',
      label: labels.range,
      data: buckets.map((bucket, index) => ({ x: xs[index], y: bucket.max })),
      hfGroup: ChartGroup.Range,
      hfLegend: false,
      hfTooltipHidden: true,
      borderWidth: 0,
      pointRadius: 0,
      fill: false,
      order: 1,
    },
    {
      type: 'line',
      label: labels.range,
      data: buckets.map((bucket, index) => ({ x: xs[index], y: bucket.min })),
      hfGroup: ChartGroup.Range,
      borderWidth: 0,
      pointRadius: 0,
      fill: '-1',
      backgroundColor: colors.band,
      order: 1,
    },
    {
      type: 'line',
      label: labels.avg,
      data: buckets.map((bucket, index) => ({ x: xs[index], y: bucket.avg })),
      hfGroup: ChartGroup.Avg,
      borderColor: colors.avg,
      borderWidth: 2,
      pointRadius: 0,
      tension: 0.2,
      fill: false,
      order: 0,
    },
    ...limitGroup(
      ChartGroup.WarningLimits,
      labels.warningLimits,
      [limits.warningLow, limits.warningHigh],
      colors.warning,
      xs,
    ),
    ...limitGroup(ChartGroup.AlarmLimits, labels.alarmLimits, [limits.alarmLow, limits.alarmHigh], colors.alarm, xs),
    {
      type: 'line',
      label: labels.signals,
      data: xs.map((x) => ({ x, y: null })),
      hfGroup: ChartGroup.Signals,
      yAxisID: EPISODE_AXIS_ID,
      borderColor: colors.muted,
      backgroundColor: colors.muted,
      borderWidth: 0,
      pointRadius: 0,
      fill: false,
      order: 3,
    },
    ...areas,
  ];

  return {
    datasets: datasets.map((dataset) => ({ ...dataset, hidden: input.hiddenGroups.has(dataset.hfGroup) })),
  };
}

/** The groups after a click on a legend entry: the group is hidden when it was shown, and shown when it was hidden. */
export function toggledGroups(hidden: ReadonlySet<string>, group: string): Set<string> {
  const next = new Set(hidden);
  if (!next.delete(group)) {
    next.add(group);
  }
  return next;
}

/** Tells whether a dataset gets its own legend entry. The other datasets of a group share the entry of the first. */
export function isLegendEntry(dataset: Pick<ChartDataset, 'hfLegend'>): boolean {
  return dataset.hfLegend !== false;
}

/** The indexes of every dataset that shares the legend group of the dataset at `index`. */
export function groupIndexes(datasets: readonly Pick<ChartDataset, 'hfGroup'>[], index: number): number[] {
  const group = datasets[index]?.hfGroup;
  return datasets.flatMap((dataset, position) => (dataset.hfGroup === group ? [position] : []));
}

/** Adds an alpha to a `#rrggbb` or `rgb(r, g, b)` color. Any other color string stays as it is. */
export function withAlpha(color: string, alpha: number): string {
  const hex = /^#([0-9a-f]{2})([0-9a-f]{2})([0-9a-f]{2})$/i.exec(color.trim());
  if (hex !== null) {
    const [red, green, blue] = hex.slice(1).map((part) => parseInt(part, 16));
    return `rgba(${red}, ${green}, ${blue}, ${alpha})`;
  }
  const rgb = /^rgb\(\s*(\d+)[\s,]+(\d+)[\s,]+(\d+)\s*\)$/i.exec(color.trim());
  if (rgb !== null) {
    return `rgba(${rgb[1]}, ${rgb[2]}, ${rgb[3]}, ${alpha})`;
  }
  return color;
}

/** The from-to of the chart view, as ISO instants. */
export interface ChartView {
  readonly from: string;
  readonly to: string;
}

/** The limits of the zoom plugin on the time axis, in epoch milliseconds. */
export interface ZoomLimits {
  readonly min: number;
  readonly max: number;
  readonly minRange: number;
}

/** The exact times that the time axis shows between `min` and `max` (epoch milliseconds), as ISO instants. */
export function scaleRange(min: number, max: number): ChartView {
  return {
    from: Temporal.Instant.fromEpochMilliseconds(Math.round(min)).toString(),
    to: Temporal.Instant.fromEpochMilliseconds(Math.round(max)).toString(),
  };
}

/**
 * The limits of the zoom: the whole `bounds`, and a view of at least 2 buckets. A bucket is as long as the bounds divided
 * by `bucketCount`.
 */
export function zoomLimits(bounds: ChartView, bucketCount: number): ZoomLimits {
  const min = epochMilliseconds(bounds.from);
  const max = epochMilliseconds(bounds.to);
  return { min, max, minRange: (MIN_ZOOM_BUCKETS * (max - min)) / Math.max(1, bucketCount) };
}

/** The format of a time on the chart: the local day, month, hour, and minute. */
export const TIME_FORMAT: Intl.DateTimeFormatOptions = {
  day: '2-digit',
  month: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
};

/** A view shorter than this shows seconds in every tick label, because minutes alone would repeat. */
const SECONDS_BELOW_SPAN_MS = 600_000;
const MS_PER_MINUTE = 60_000;

/**
 * The label of a tick of the time axis: the value (epoch milliseconds) in local time in the zone. A `spanMs` below 10
 * minutes adds the seconds, and so does a value that is not on a whole minute (an end of the view).
 */
export function tickLabel(value: number, timeZoneId: string, spanMs: number): string {
  const instant = Temporal.Instant.fromEpochMilliseconds(Math.round(value));
  const hasSeconds = spanMs < SECONDS_BELOW_SPAN_MS || instant.epochMilliseconds % MS_PER_MINUTE !== 0;
  const format = hasSeconds ? { ...TIME_FORMAT, second: '2-digit' as const } : TIME_FORMAT;
  return instant.toZonedDateTimeISO(timeZoneId).toLocaleString(undefined, format);
}

const MS_PER_SECOND = 1000;
const MS_PER_HOUR = 3_600_000;
const MS_PER_DAY = 24 * MS_PER_HOUR;
const ENDS_COUNT = 2;

/** The calendar steps below one day, from the finest: 1, 5, 10, 30 seconds; 1, 5, 10, 15, 30 minutes; 1, 2, 3, 6, 12 hours. */
const SUB_DAY_STEPS_MS: readonly number[] = [
  ...[1, 5, 10, 30].map((seconds) => seconds * MS_PER_SECOND),
  ...[1, 5, 10, 15, 30].map((minutes) => minutes * MS_PER_MINUTE),
  ...[1, 2, 3, 6, 12].map((hours) => hours * MS_PER_HOUR),
];
/** The steps in whole days after the sub-day steps: 1 day, 2 days, and 1 week. */
const DAY_STEPS: readonly number[] = [1, 2, 7];
/** The share of the view on each side where an inner tick would crowd the label of an end. */
const END_CROWD_SHARE = 0.05;

function subDayTicks(min: number, max: number, timeZoneId: string, stepMs: number): number[] {
  const offsetMs =
    Temporal.Instant.fromEpochMilliseconds(Math.round(min)).toZonedDateTimeISO(timeZoneId).offsetNanoseconds / 1e6;
  const ticks: number[] = [];
  for (let tick = Math.ceil((min + offsetMs) / stepMs) * stepMs - offsetMs; tick <= max; tick += stepMs) {
    ticks.push(tick);
  }
  return ticks;
}

function dayTicks(min: number, max: number, timeZoneId: string, days: number): number[] {
  const first = Temporal.Instant.fromEpochMilliseconds(Math.round(min)).toZonedDateTimeISO(timeZoneId).startOfDay();
  const ticks: number[] = [];
  for (let day = first; day.epochMilliseconds <= max; day = day.add({ days })) {
    if (day.epochMilliseconds >= min) {
      ticks.push(day.epochMilliseconds);
    }
  }
  return ticks;
}

/**
 * The ticks of the time axis: the 2 ends of the view, and between them the ticks at round local times of the smallest
 * calendar step that keeps at most `maxTicks` ticks in all. The step is chosen by the count first, and only its ticks
 * are built, so a very long view costs the same as a short one. An inner tick near an end is left out, so it does not hide
 * the label of the end. A step uses the offset of the zone at the start of the view, so a change of the offset inside
 * the view moves the ticks after it.
 */
export function timeTicks(min: number, max: number, timeZoneId: string, maxTicks: number): number[] {
  const crowd = (max - min) * END_CROWD_SHARE;
  const fits = (stepMs: number): boolean => (max - min) / stepMs <= maxTicks - ENDS_COUNT;
  const subDayStep = SUB_DAY_STEPS_MS.find(fits);
  const days = DAY_STEPS.find((candidate) => fits(candidate * MS_PER_DAY)) ?? widestDayStep(min, max, maxTicks);
  const ticks =
    subDayStep === undefined ? dayTicks(min, max, timeZoneId, days) : subDayTicks(min, max, timeZoneId, subDayStep);
  const inner = ticks.filter((tick) => tick - min >= crowd && max - tick >= crowd).slice(0, maxTicks - ENDS_COUNT);
  return [min, ...inner, max];
}

/** The whole days of a step for a view that is longer than the longest standard step allows: it fits `maxTicks`. */
function widestDayStep(min: number, max: number, maxTicks: number): number {
  return Math.ceil((max - min) / MS_PER_DAY / (maxTicks - ENDS_COUNT));
}

/** The `rgba(r, g, b, a)` color without its alpha. Any other color string stays as it is. */
export function withoutAlpha(color: string): string {
  const rgba = /^rgba\(\s*(\d+)[\s,]+(\d+)[\s,]+(\d+)[\s,/]+[\d.]+\s*\)$/i.exec(color.trim());
  return rgba === null ? color : `rgb(${rgba[1]}, ${rgba[2]}, ${rgba[3]})`;
}

/** One signal of an area: the buckets that it covers and its tooltip text. */
export interface AreaSignal {
  readonly first: number;
  readonly last: number;
  readonly text: string;
}

/** Signals that overlap or touch, drawn as one area. The times are epoch milliseconds. */
export interface SignalArea {
  readonly from: number;
  readonly to: number;
  readonly severity: SignalSeverity;
  readonly episodes: readonly SignalEpisode[];
}

/**
 * Joins the signals whose times overlap or touch into areas. An open signal ends at `viewEnd`. An area has the highest
 * severity of its signals.
 */
export function signalAreas(episodes: readonly SignalEpisode[], viewEnd: number): SignalArea[] {
  const spans = episodes
    .map((episode) => ({
      episode,
      from: epochMilliseconds(episode.openedAt),
      to: episode.resolvedAt === null ? viewEnd : epochMilliseconds(episode.resolvedAt),
    }))
    .sort((a, b) => a.from - b.from);
  const areas: SignalArea[] = [];
  for (const span of spans) {
    const last = areas.at(-1);
    if (last !== undefined && span.from <= last.to) {
      areas[areas.length - 1] = {
        from: last.from,
        to: Math.max(last.to, span.to),
        severity: highestSeverity(last.severity, span.episode.severity),
        episodes: [...last.episodes, span.episode],
      };
    } else {
      areas.push({ from: span.from, to: span.to, severity: span.episode.severity, episodes: [span.episode] });
    }
  }
  return areas;
}

function highestSeverity(first: SignalSeverity, second: SignalSeverity): SignalSeverity {
  return first === SignalSeverity.Alarm || second === SignalSeverity.Alarm
    ? SignalSeverity.Alarm
    : SignalSeverity.Warning;
}
