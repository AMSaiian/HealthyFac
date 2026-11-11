import { type Signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { map, timer } from 'rxjs';

import type { ScheduleBar, ScheduleBlock, ScheduleRow } from '@shared/schedule/schedule-row';
import type { Clock } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import type { DaySchedule, DayScheduleBlock, DayScheduleWorkOrder } from '@work-orders/work-order';
import { STATUS_LABEL_KEYS } from '@work-orders/list/work-order-grid/work-order-grid.utils';

const MINUTE_MS = 60_000;
const STATUS_CLASS_PREFIX = 'hf-status-';

export function toBlocks(blocks: readonly DayScheduleBlock[]): ScheduleBlock[] {
  return blocks.map((block) => ({
    start: Temporal.Instant.from(block.startUtc),
    end: Temporal.Instant.from(block.endUtc),
  }));
}

export function toBars(workOrders: readonly DayScheduleWorkOrder[]): ScheduleBar[] {
  return workOrders.map((workOrder) => ({
    id: workOrder.id,
    title: workOrder.title,
    start: Temporal.Instant.from(workOrder.plannedStartUtc),
    end: Temporal.Instant.from(workOrder.occupancyEndUtc),
    statusKey: STATUS_LABEL_KEYS[workOrder.status],
    colorClass: `${STATUS_CLASS_PREFIX}${workOrder.status}`,
  }));
}

/** Maps the day of the backend to one row of the schedule board, with every time as a Temporal value. */
export function toRow(id: string, label: string, day: DaySchedule): ScheduleRow {
  return { id, label, blocks: toBlocks(day.blocks), bars: toBars(day.workOrders) };
}

export function todayOf(clock: Clock): Temporal.PlainDate {
  return clock.now().toZonedDateTimeISO(clock.timeZoneId()).toPlainDate();
}

/**
 * A signal of the current instant that ticks on each full minute, so the "now" line moves on the minute. The first
 * tick comes after the rest of the current minute. Call it in an injection context.
 */
export function minuteTicker(clock: Clock): Signal<Temporal.Instant> {
  const restOfMinute = MINUTE_MS - (clock.now().epochMilliseconds % MINUTE_MS);
  return toSignal(timer(restOfMinute, MINUTE_MS).pipe(map(() => clock.now())), { initialValue: clock.now() });
}

/** Moves a shown date that was today to the new today when the day changes. Any other shown date stays. */
export function followToday(
  shown: Temporal.PlainDate | null,
  previousToday: Temporal.PlainDate,
  today: Temporal.PlainDate,
): Temporal.PlainDate | null {
  return shown !== null && shown.equals(previousToday) && !today.equals(previousToday) ? today : shown;
}
