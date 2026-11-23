import { Temporal } from '@shared/time/temporal';

import type { ScheduleBlock } from '@shared/schedule/schedule-row';

/** The time that a bar or a planned slot occupies. */
export interface Slot {
  readonly start: Temporal.Instant;
  readonly end: Temporal.Instant;
}

function isInsideOneBlock(slot: Slot, blocks: readonly ScheduleBlock[]): boolean {
  return blocks.some(
    (block) =>
      Temporal.Instant.compare(block.start, slot.start) <= 0 && Temporal.Instant.compare(slot.end, block.end) <= 0,
  );
}

function isOverlapping(first: Slot, second: Slot): boolean {
  return Temporal.Instant.compare(first.start, second.end) < 0 && Temporal.Instant.compare(second.start, first.end) < 0;
}

/**
 * Whether a slot breaks a rule of its row: it lies outside every work block, or it overlaps another slot of the row
 * (half-open intervals). The pending bars and the drag preview share this definition.
 */
export function isSlotBroken(slot: Slot, blocks: readonly ScheduleBlock[], others: readonly Slot[]): boolean {
  return !isInsideOneBlock(slot, blocks) || others.some((other) => isOverlapping(slot, other));
}
