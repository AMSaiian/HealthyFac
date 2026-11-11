import type { ShiftBlock } from '@employees/shifts/shift';
import { BLOCK_LATEST_END } from '@employees/shifts/shift-limits';

const FIRST_BLOCK: ShiftBlock = { start: '08:00', end: '12:00' };
const MINUTES_PER_HOUR = 60;
const NEW_BLOCK_MINUTES = 60;

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
 * last one ends and runs one hour, at most to 23:55. The result is `null` when the last block already ends at 23:55.
 */
export function nextBlock(blocks: readonly ShiftBlock[]): ShiftBlock | null {
  const last = blocks.at(-1);
  if (last === undefined) {
    return FIRST_BLOCK;
  }
  const latest = minutesOf(BLOCK_LATEST_END);
  const start = minutesOf(last.end);
  return start >= latest ? null : { start: last.end, end: timeOf(Math.min(start + NEW_BLOCK_MINUTES, latest)) };
}
