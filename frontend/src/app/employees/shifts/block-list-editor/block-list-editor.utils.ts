import { Temporal } from '@shared/time/temporal';
import { toInstant } from '@shared/time/zoned-times';

import type { ShiftBlock } from '@employees/shifts/shift';
import { BLOCK_GRID_MINUTES, BLOCK_LATEST_END } from '@employees/shifts/shift-limits';

const FIRST_BLOCK_START = '08:00';
const MINUTES_PER_HOUR = 60;
const NEW_BLOCK_MINUTES = 60;
const FIRST_BLOCK_MINUTES = 240;

/** The state of a block of today: it ended, it runs now, or it starts later. Another date has only `future` blocks. */
export type BlockState = 'ended' | 'running' | 'future';

function minutesOf(time: string): number {
  const [hours, minutes] = time.split(':').map(Number);
  return hours * MINUTES_PER_HOUR + minutes;
}

function timeOf(totalMinutes: number): string {
  const hours = Math.floor(totalMinutes / MINUTES_PER_HOUR);
  const minutes = totalMinutes % MINUTES_PER_HOUR;
  return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`;
}

/**
 * The block that **Add block** appends: the first block of a day is 08:00 to 12:00; a later block starts where the
 * last one ends and runs one hour, at most to 23:55. A new block never starts before `earliest` (now on the grid for
 * today). The result is `null` when no block fits before 23:55.
 */
export function nextBlock(blocks: readonly ShiftBlock[], earliest = '00:00'): ShiftBlock | null {
  const last = blocks.at(-1);
  const latest = minutesOf(BLOCK_LATEST_END);
  const start = Math.max(minutesOf(last?.end ?? FIRST_BLOCK_START), minutesOf(earliest));
  if (start >= latest) {
    return null;
  }
  const length = last === undefined ? FIRST_BLOCK_MINUTES : NEW_BLOCK_MINUTES;
  return { start: timeOf(start), end: timeOf(Math.min(start + length, latest)) };
}

/**
 * Gives the state of a block by instants. Only a date equal to today in the zone has `ended` and `running` blocks:
 * a block that ends at or before now is `ended`, and a block that starts at or before now is `running`.
 */
export function blockStateOf(
  block: ShiftBlock,
  date: Temporal.PlainDate | null,
  now: Temporal.Instant,
  timeZoneId: string,
): BlockState {
  if (date === null || !date.equals(now.toZonedDateTimeISO(timeZoneId).toPlainDate())) {
    return 'future';
  }
  const end = toInstant(date, Temporal.PlainTime.from(block.end), timeZoneId);
  if (Temporal.Instant.compare(end, now) <= 0) {
    return 'ended';
  }
  const start = toInstant(date, Temporal.PlainTime.from(block.start), timeZoneId);
  return Temporal.Instant.compare(start, now) <= 0 ? 'running' : 'future';
}

/**
 * Rounds the local time of now up to the next 5-minute step (13:02 gives 13:05, 13:05 stays). Like the backend, the
 * result is on the grid of the zone. After 23:55 the result is 23:55, the latest time of a block: the rounding would
 * wrap to 00:00.
 */
export function nowOnGrid(now: Temporal.Instant, timeZoneId: string): Temporal.PlainTime {
  const time = now.toZonedDateTimeISO(timeZoneId).toPlainTime();
  const latest = Temporal.PlainTime.from(BLOCK_LATEST_END);
  return Temporal.PlainTime.compare(time, latest) > 0
    ? latest
    : time.round({ smallestUnit: 'minute', roundingIncrement: BLOCK_GRID_MINUTES, roundingMode: 'ceil' });
}
