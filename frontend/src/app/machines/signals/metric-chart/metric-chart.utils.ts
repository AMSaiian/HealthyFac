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
  /** The color of the legend entry of the signal strip. */
  readonly muted: string;
}

/**
 * One dataset of the chart. `hfGroup` names the legend entry that the dataset belongs to: a click on the entry shows or
 * hides the whole group. `hfLegend: false` keeps the dataset out of the legend, because its group has another dataset
 * that carries the entry. `hfTooltip` is the tooltip text of a strip segment.
 */
export interface ChartDataset {
  readonly type: 'line';
  readonly label: string;
  readonly data: (number | null)[];
  readonly hfGroup: string;
  readonly hfLegend?: false;
  readonly hfTooltip?: string;
  readonly [option: string]: unknown;
}

export interface ChartDataModel {
  labels: string[];
  datasets: ChartDataset[];
}

export interface ChartInput {
  readonly buckets: readonly MetricBucket[];
  readonly limits: ChartLimits;
  readonly episodes: readonly SignalEpisode[];
  readonly colors: ChartColors;
  readonly labels: ChartLabels;
  readonly formatTime: (iso: string) => string;
  /** The tooltip text of an episode: its rule, severity, metric, and local start and end. */
  readonly episodeText: (episode: SignalEpisode) => string;
  /** The legend groups that the user hid. The chart shows the warning limits hidden at first (`DEFAULT_HIDDEN_GROUPS`). */
  readonly hiddenGroups: ReadonlySet<string>;
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

/** The groups that start hidden: the warning limits show after a click on their legend entry. */
export const DEFAULT_HIDDEN_GROUPS: ReadonlySet<string> = new Set([ChartGroup.WarningLimits]);

/** The id of the hidden axis of the signal strip. */
export const EPISODE_AXIS_ID = 'episodes';

/** The height of the strip on the hidden axis (0 to 1): a thin line under the plot. */
const STRIP_LEVEL = 0.03;
const STRIP_WIDTH = 6;
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
  count: number,
  isFirst: boolean,
): ChartDataset {
  return {
    type: 'line',
    label,
    data: Array.from({ length: count }, () => value),
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
  count: number,
): ChartDataset[] {
  return values
    .filter((value): value is number => value !== null)
    .map((value, index) => limitLine(group, label, value, color, count, index === 0));
}

function stripSegment(
  episode: SignalEpisode,
  range: { readonly first: number; readonly last: number },
  count: number,
  input: ChartInput,
): ChartDataset {
  const isAlarm = episode.severity === SignalSeverity.Alarm;
  return {
    type: 'line',
    label: input.labels.signals,
    data: Array.from({ length: count }, (_, index) =>
      index >= range.first && index <= range.last ? STRIP_LEVEL : null,
    ),
    hfGroup: ChartGroup.Signals,
    hfLegend: false,
    hfTooltip: input.episodeText(episode),
    yAxisID: EPISODE_AXIS_ID,
    borderColor: isAlarm ? input.colors.alarm : input.colors.warning,
    borderWidth: STRIP_WIDTH,
    pointRadius: 0,
    fill: false,
    order: 3,
  };
}

/**
 * Maps the history to the datasets of the chart, in 5 legend entries: the average line, the range band between the
 * minimum and the maximum (no lines of its own), the warning limits (hidden at first), the alarm limits, and the signals
 * (a thin strip with one segment per episode, in the color of its severity).
 */
export function toChartData(input: ChartInput): ChartDataModel {
  const { buckets, limits, colors, labels } = input;
  const count = buckets.length;
  const segments = input.episodes.flatMap((episode) => {
    const range = episodeBucketRange(buckets, episode);
    return range === null ? [] : [stripSegment(episode, range, count, input)];
  });

  const datasets: ChartDataset[] = [
    {
      type: 'line',
      label: labels.range,
      data: buckets.map((bucket) => bucket.max),
      hfGroup: ChartGroup.Range,
      hfLegend: false,
      borderWidth: 0,
      pointRadius: 0,
      fill: false,
      order: 1,
    },
    {
      type: 'line',
      label: labels.range,
      data: buckets.map((bucket) => bucket.min),
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
      data: buckets.map((bucket) => bucket.avg),
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
      count,
    ),
    ...limitGroup(ChartGroup.AlarmLimits, labels.alarmLimits, [limits.alarmLow, limits.alarmHigh], colors.alarm, count),
    {
      type: 'line',
      label: labels.signals,
      data: Array.from({ length: count }, () => null),
      hfGroup: ChartGroup.Signals,
      yAxisID: EPISODE_AXIS_ID,
      borderColor: colors.muted,
      backgroundColor: colors.muted,
      borderWidth: STRIP_WIDTH,
      pointRadius: 0,
      fill: false,
      order: 3,
    },
    ...segments,
  ];

  return {
    labels: buckets.map((bucket) => input.formatTime(bucket.start)),
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
