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
const MS_PER_MINUTE = 60_000;
const MINUTES_PER_HOUR = 60;

/** The grid of planned starts and estimates in minutes. The backend rounds to the same grid. */
export const SCHEDULE_GRID_MINUTES = 5;
/** The shortest and the longest estimate that a resize may give, in minutes. The backend checks the same limits. */
export const RESIZE_MIN_MINUTES = 5;
export const RESIZE_MAX_MINUTES = 12 * MINUTES_PER_HOUR;
/** How long a pointer rests on a day switch area before the day changes, in milliseconds. */
export const DAY_SWITCH_DWELL_MS = 600;

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

/** The instant at a position of the axis in percent. It is the reverse of `percentOf`. */
export function instantAt(percent: number, axis: Axis): Temporal.Instant {
  return Temporal.Instant.fromEpochMilliseconds(
    Math.round(axis.start.epochMilliseconds + (percent / PERCENT) * lengthMs(axis)),
  );
}

/** Rounds an instant to the nearest grid step. The zone offsets are whole multiples of the step, so UTC and local agree. */
export function snapToGrid(instant: Temporal.Instant): Temporal.Instant {
  const step = SCHEDULE_GRID_MINUTES * MS_PER_MINUTE;
  return Temporal.Instant.fromEpochMilliseconds(Math.round(instant.epochMilliseconds / step) * step);
}

/** The estimate of a bar of a width in percent of the axis: snapped to the grid and clamped to the allowed range. */
export function minutesOfWidth(widthPercent: number, axis: Axis): number {
  const minutes = (widthPercent / PERCENT) * (lengthMs(axis) / MS_PER_MINUTE);
  const snapped = Math.round(minutes / SCHEDULE_GRID_MINUTES) * SCHEDULE_GRID_MINUTES;
  return Math.min(RESIZE_MAX_MINUTES, Math.max(RESIZE_MIN_MINUTES, snapped));
}

/** The width of an estimate in percent of the axis. The staging area draws its bars with it. */
export function widthPercentOf(estimateMinutes: number, axis: Axis): number {
  return ((estimateMinutes * MS_PER_MINUTE) / lengthMs(axis)) * PERCENT;
}

/**
 * Runs a callback once after a pointer rests on a day switch area. `enter` starts the wait, `leave` cancels it, and the
 * wait starts again after each run while the pointer stays.
 */
export function createDwell(onElapsed: () => void, delayMs = DAY_SWITCH_DWELL_MS): { enter(): void; leave(): void } {
  let timer: ReturnType<typeof setTimeout> | null = null;
  const leave = (): void => {
    if (timer !== null) {
      clearTimeout(timer);
      timer = null;
    }
  };
  const enter = (): void => {
    if (timer !== null) {
      return;
    }
    timer = setTimeout(() => {
      timer = null;
      onElapsed();
      enter();
    }, delayMs);
  };
  return { enter, leave };
}

/** The data attribute values that mark a row of the board and the staging area for the drop lookup. */
export const ROW_ATTRIBUTE = 'data-row-id';
export const STAGING_ATTRIBUTE = 'data-staging';

export type DropTarget = { readonly kind: 'row'; readonly rowId: string } | { readonly kind: 'staging' } | null;

/** Finds what lies under the pointer: a row of the board, the staging area, or nothing. */
export function dropTargetOf(elements: readonly Element[]): DropTarget {
  for (const element of elements) {
    const staging = element.closest(`[${STAGING_ATTRIBUTE}]`);
    if (staging !== null) {
      return { kind: 'staging' };
    }
    const row = element.closest(`[${ROW_ATTRIBUTE}]`);
    const rowId = row?.getAttribute(ROW_ATTRIBUTE);
    if (rowId !== null && rowId !== undefined) {
      return { kind: 'row', rowId };
    }
  }
  return null;
}

/** The layout of the board in rem. The templates bind them to CSS custom properties, so the CSS and the sums agree. */
export const NAME_COLUMN_REM = 10;
export const ROW_PADDING_REM = 0.9375;
export const LANE_HEIGHT_REM = 1.5;
export const LANE_GAP_REM = 0.25;

/** The height of a row in rem for a number of lanes. The CSS of the board follows the same sum. */
export function rowHeightRem(laneCount: number): number {
  return 2 * ROW_PADDING_REM + laneCount * LANE_HEIGHT_REM + (laneCount - 1) * LANE_GAP_REM;
}

/** The top of a bar in the editable overlay: the rows above it, the row padding, and the lanes above it. */
export function barTopCss(rowLaneCounts: readonly number[], rowIndex: number, lane: number): string {
  const rem =
    rowLaneCounts.slice(0, rowIndex).reduce((sum, count) => sum + rowHeightRem(count), 0) +
    ROW_PADDING_REM +
    lane * (LANE_HEIGHT_REM + LANE_GAP_REM);
  return `calc(${rem}rem + ${rowIndex}px)`;
}
