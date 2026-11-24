import type { ScheduleBar, ScheduleBlock, ScheduleRow } from '@shared/schedule/schedule-row';
import { Temporal } from '@shared/time/temporal';

import {
  type DaySchedule,
  type DayScheduleBlock,
  type DayScheduleWorkOrder,
  WorkOrderStatus,
} from '@work-orders/work-order';
import { STATUS_LABEL_KEYS } from '@work-orders/list/work-order-grid/work-order-grid.utils';

const STATUS_CLASS_PREFIX = 'hf-status-';
const ISO_DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/;

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
    isLocked: workOrder.status === WorkOrderStatus.Completed,
    isRunning: workOrder.status === WorkOrderStatus.InProgress,
    plannedEnd: Temporal.Instant.from(workOrder.plannedStartUtc).add({ minutes: workOrder.estimateMinutes }),
  }));
}

/** Maps the day of the backend to one row of the schedule board, with every time as a Temporal value. */
export function toRow(id: string, label: string, day: DaySchedule): ScheduleRow {
  return { id, label, blocks: toBlocks(day.blocks), bars: toBars(day.workOrders) };
}

/** Reads a `yyyy-MM-dd` text. A missing, malformed, or impossible date gives `null`. */
export function plainDateOrNull(value: string | undefined): Temporal.PlainDate | null {
  if (value === undefined || !ISO_DATE_PATTERN.test(value)) {
    return null;
  }
  try {
    return Temporal.PlainDate.from(value, { overflow: 'reject' });
  } catch {
    return null;
  }
}

/** Moves a shown date that was today to the new today when the day changes. Any other shown date stays. */
export function followToday(
  shown: Temporal.PlainDate | null,
  previousToday: Temporal.PlainDate,
  today: Temporal.PlainDate,
): Temporal.PlainDate | null {
  return shown !== null && shown.equals(previousToday) && !today.equals(previousToday) ? today : shown;
}
