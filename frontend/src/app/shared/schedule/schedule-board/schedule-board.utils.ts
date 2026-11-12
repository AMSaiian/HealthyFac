import { SCHEDULE_SKELETON_ROWS } from '@shared/loading/loading-constants';
import { Temporal } from '@shared/time/temporal';
import { dayRange } from '@shared/time/zoned-times';

import type { ScheduleBar, ScheduleBlock } from '@shared/schedule/schedule-row';

/** The axis of one day: from the start of the date to the start of the next date, with the real length. */
export interface Axis {
  readonly start: Temporal.Instant;
  readonly end: Temporal.Instant;
}

export interface Segment {
  readonly left: number;
  readonly width: number;
}

export interface PlacedBar {
  readonly bar: ScheduleBar;
  readonly left: number;
  readonly width: number;
  readonly lane: number;
}

const PERCENT = 100;

export function axisOf(date: Temporal.PlainDate, timeZoneId: string): Axis {
  return dayRange(date, timeZoneId);
}

export function lengthMs(axis: Axis): number {
  return axis.end.epochMilliseconds - axis.start.epochMilliseconds;
}

/** The position of an instant on the axis in percent. It is not clipped. */
export function percentOf(instant: Temporal.Instant, axis: Axis): number {
  return ((instant.epochMilliseconds - axis.start.epochMilliseconds) / lengthMs(axis)) * PERCENT;
}

function clamp(value: number): number {
  return Math.min(PERCENT, Math.max(0, value));
}

/**
 * The label of each local hour that exists on the day. A real hour that repeats at the autumn change gets two labels;
 * the skipped hour of the spring change gets none. The count is the number of hour columns of the board.
 */
export function hourLabelsOf(axis: Axis, timeZoneId: string): string[] {
  const labels: string[] = [];
  let hour = axis.start.toZonedDateTimeISO(timeZoneId).round({ smallestUnit: 'hour', roundingMode: 'ceil' });
  while (Temporal.Instant.compare(hour.toInstant(), axis.end) < 0) {
    labels.push(hour.toPlainTime().toString({ smallestUnit: 'minute' }));
    hour = hour.add({ hours: 1 });
  }
  return labels;
}

/** The part of a time range that lies on the axis, in percent, or `null` when the range lies outside. */
export function segmentOf(start: Temporal.Instant, end: Temporal.Instant, axis: Axis): Segment | null {
  const left = clamp(percentOf(start, axis));
  const right = clamp(percentOf(end, axis));
  return right > left ? { left, width: right - left } : null;
}

/** The shaded parts of the axis: everything outside the blocks. A day without blocks is shaded completely. */
export function gapsOf(blocks: readonly ScheduleBlock[], axis: Axis): Segment[] {
  const ordered = [...blocks].sort((a, b) => Temporal.Instant.compare(a.start, b.start));
  const gaps: Segment[] = [];
  let cursor = axis.start;
  for (const block of ordered) {
    const gap = segmentOf(cursor, block.start, axis);
    if (gap !== null) {
      gaps.push(gap);
    }
    cursor = Temporal.Instant.compare(block.end, cursor) > 0 ? block.end : cursor;
  }
  const last = segmentOf(cursor, axis.end, axis);
  return last === null ? gaps : [...gaps, last];
}

/**
 * Places the bars: the position in percent, clipped to the axis, and the first lane in which no earlier bar overlaps.
 * Returns the placed bars and the number of lanes.
 */
export function placeBars(bars: readonly ScheduleBar[], axis: Axis): { placed: PlacedBar[]; laneCount: number } {
  const ordered = [...bars].sort((a, b) => Temporal.Instant.compare(a.start, b.start));
  const laneEnds: Temporal.Instant[] = [];
  const placed: PlacedBar[] = [];
  for (const bar of ordered) {
    const segment = segmentOf(bar.start, bar.end, axis);
    if (segment === null) {
      continue;
    }
    let lane = laneEnds.findIndex((end) => Temporal.Instant.compare(end, bar.start) <= 0);
    if (lane === -1) {
      lane = laneEnds.length;
    }
    laneEnds[lane] = bar.end;
    placed.push({ bar, left: segment.left, width: segment.width, lane });
  }
  return { placed, laneCount: Math.max(1, laneEnds.length) };
}

/** The position of "now" on the axis in percent, or `null` when now is not on the day. */
export function nowPercentOf(now: Temporal.Instant, axis: Axis): number | null {
  const percent = percentOf(now, axis);
  return percent >= 0 && percent < PERCENT ? percent : null;
}

/** The skeleton rows of a loading day: the rows shown before the load, or the default count when none were shown. */
export function skeletonRowCount(previousCount: number): number {
  return previousCount > 0 ? previousCount : SCHEDULE_SKELETON_ROWS;
}

/** The tooltip of a bar on two lines: the title, then the status, the local range, and the offset. */
export function tooltipOf(bar: ScheduleBar, statusLabel: string, timeZoneId: string): string {
  return `${bar.title}\n${statusLabel} · ${describeBar(bar, timeZoneId)}`;
}

/** The local start and end of a bar with the offset of the zone, e.g. `09:00 - 10:00 (+03:00)`. */
export function describeBar(bar: ScheduleBar, timeZoneId: string): string {
  const start = bar.start.toZonedDateTimeISO(timeZoneId);
  const end = bar.end.toZonedDateTimeISO(timeZoneId);
  const time = (value: Temporal.ZonedDateTime): string => value.toPlainTime().toString({ smallestUnit: 'minute' });
  return `${time(start)} - ${time(end)} (${start.offset})`;
}
