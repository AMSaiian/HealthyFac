import { Temporal } from '@shared/time/temporal';

import type { DaySchedule } from '@work-orders/work-order';

export const GRID_STEP_MINUTES = 5;

const MS_PER_MINUTE = 60_000;
const GRID_STEP_MS = GRID_STEP_MINUTES * MS_PER_MINUTE;

/** A work block of the chosen day, as instants. */
export interface PrefillBlock {
  readonly start: Temporal.Instant;
  readonly end: Temporal.Instant;
}

/** The time that a scheduled work order occupies: from the planned start to `occupancyEndUtc` of the backend. */
export interface PrefillOccupancy {
  readonly start: Temporal.Instant;
  readonly end: Temporal.Instant;
}

export interface PrefillDay {
  readonly blocks: readonly PrefillBlock[];
  readonly occupancies: readonly PrefillOccupancy[];
}

function roundUpToGrid(milliseconds: number): number {
  return Math.ceil(milliseconds / GRID_STEP_MS) * GRID_STEP_MS;
}

/**
 * Finds the first start on the grid step, at or after `now`, where a work order of `estimateMinutes` fits inside one
 * block of the day and overlaps no occupancy. A start may equal the end of an occupancy. Returns `null` when no
 * start is left on the day. The grid is in UTC, which equals the local grid because every zone offset is a multiple of
 * 5 minutes.
 */
export function firstFreeStart(
  day: PrefillDay,
  estimateMinutes: number,
  now: Temporal.Instant,
): Temporal.Instant | null {
  const slotMs = estimateMinutes * MS_PER_MINUTE;
  const blocks = [...day.blocks].sort((a, b) => a.start.epochMilliseconds - b.start.epochMilliseconds);

  for (const block of blocks) {
    let start = roundUpToGrid(Math.max(block.start.epochMilliseconds, now.epochMilliseconds));
    while (start + slotMs <= block.end.epochMilliseconds) {
      const slotEnd = start + slotMs;
      const blockerEnd = Math.max(
        ...day.occupancies
          .filter((occupancy) => occupancy.start.epochMilliseconds < slotEnd && start < occupancy.end.epochMilliseconds)
          .map((occupancy) => occupancy.end.epochMilliseconds),
        Number.NEGATIVE_INFINITY,
      );
      if (blockerEnd === Number.NEGATIVE_INFINITY) {
        return Temporal.Instant.fromEpochMilliseconds(start);
      }
      start = roundUpToGrid(blockerEnd);
    }
  }

  return null;
}

/**
 * Maps the day of the backend to the instants that `firstFreeStart` reads. The work order that the user moves does not
 * occupy its own new slot, so the caller passes its ID to leave it out, like `ScheduleChecks` of the backend does.
 */
export function toPrefillDay(schedule: DaySchedule, excludedWorkOrderId?: string): PrefillDay {
  return {
    blocks: schedule.blocks.map((block) => ({
      start: Temporal.Instant.from(block.startUtc),
      end: Temporal.Instant.from(block.endUtc),
    })),
    occupancies: schedule.workOrders
      .filter((workOrder) => workOrder.id !== excludedWorkOrderId)
      .map((workOrder) => ({
        start: Temporal.Instant.from(workOrder.plannedStartUtc),
        end: Temporal.Instant.from(workOrder.occupancyEndUtc),
      })),
  };
}
