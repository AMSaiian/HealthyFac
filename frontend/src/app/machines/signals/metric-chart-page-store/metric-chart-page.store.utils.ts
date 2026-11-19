import { type MetricDefinition, MetricValueKind } from '@machines/metrics/metric-definition';
import { ChartRange, type MetricHistoryResponse } from '@machines/signals/signal-episode';
import type { PlainDateTimeRange } from '@shared/time/plain-date-time-range';
import { Temporal } from '@shared/time/temporal';

/** The most buckets that the chart asks for. It is the API cap of the buckets of one history. */
export const MAX_BUCKET_COUNT = 500;

const RANGE_HOURS: Readonly<Record<ChartRange, number>> = {
  [ChartRange.Hour]: 1,
  [ChartRange.SixHours]: 6,
  [ChartRange.Day]: 24,
};

export const RANGE_LABEL_KEYS: Readonly<Record<ChartRange, string>> = {
  [ChartRange.Hour]: 'SIGNALS.RANGES.HOUR',
  [ChartRange.SixHours]: 'SIGNALS.RANGES.SIX_HOURS',
  [ChartRange.Day]: 'SIGNALS.RANGES.DAY',
};

/** The buckets for a chart width in CSS pixels: 1 bucket per pixel, at least 1 and at most `MAX_BUCKET_COUNT`. */
export function bucketCountForWidth(width: number): number {
  return Math.min(MAX_BUCKET_COUNT, Math.max(1, Math.floor(width)));
}

/** The time window of a range that ends at `now`. The times are ISO instants. */
export function rangeBounds(now: Temporal.Instant, range: ChartRange): { from: string; to: string } {
  return { from: now.subtract({ hours: RANGE_HOURS[range] }).toString(), to: now.toString() };
}

/** The range of the chart: a preset that ends now, or a custom from-to (ISO instants). */
export type RangeSelection =
  | { readonly kind: 'preset'; readonly preset: ChartRange }
  | { readonly kind: 'custom'; readonly from: string; readonly to: string };

export const DEFAULT_RANGE: RangeSelection = { kind: 'preset', preset: ChartRange.Hour };

/** The pause after the last range change (a pick, a zoom, a pan) before the chart asks for the history (Artur). */
export const RANGE_DEBOUNCE_MS = 300;

/** The time window of a range selection. A preset ends at `now`. */
export function selectionBounds(now: Temporal.Instant, selection: RangeSelection): { from: string; to: string } {
  return selection.kind === 'preset' ? rangeBounds(now, selection.preset) : { from: selection.from, to: selection.to };
}

/**
 * The UTC from-to of a picked range of local moments in the zone, or `null` while an end is missing or the start is not
 * before the end.
 */
export function customBounds(range: PlainDateTimeRange, timeZoneId: string): { from: string; to: string } | null {
  if (range.from === null || range.to === null) {
    return null;
  }
  const from = range.from.toZonedDateTime(timeZoneId, { disambiguation: 'compatible' }).toInstant();
  const to = range.to.toZonedDateTime(timeZoneId, { disambiguation: 'compatible' }).toInstant();
  return Temporal.Instant.compare(from, to) < 0 ? { from: from.toString(), to: to.toString() } : null;
}

/** The local moments of a custom range, for the picker. */
export function toPlainRange(selection: RangeSelection, timeZoneId: string): PlainDateTimeRange {
  if (selection.kind === 'preset') {
    return { from: null, to: null };
  }
  const local = (iso: string): Temporal.PlainDateTime =>
    Temporal.Instant.from(iso).toZonedDateTimeISO(timeZoneId).toPlainDateTime();
  return { from: local(selection.from), to: local(selection.to) };
}

/** The metrics that the chart can draw: the numeric ones. */
export function chartableMetrics(metrics: readonly MetricDefinition[]): readonly MetricDefinition[] {
  return metrics.filter((metric) => metric.valueKind === MetricValueKind.Numeric);
}

/** The buckets of a history with the times that the load asked for, so the chart axis shows exactly that view. */
export interface LoadedHistory extends MetricHistoryResponse {
  readonly from: string;
  readonly to: string;
}
