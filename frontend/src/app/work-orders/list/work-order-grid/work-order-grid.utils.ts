import { asList, asRange, asText } from '@shared/grid/table/table-filters';
import { isRangeEmpty } from '@shared/time/plain-date-range';

import {
  WorkOrderCategory,
  WorkOrderColumn,
  type WorkOrderFilter,
  WorkOrderPriority,
  WorkOrderSortColumn,
  WorkOrderStatus,
} from '@work-orders/work-order';

export type FilterableColumn = Exclude<WorkOrderColumn, WorkOrderColumn.Estimate | WorkOrderColumn.CreatedAt>;

export const FILTERABLE_COLUMNS: readonly FilterableColumn[] = [
  WorkOrderColumn.Title,
  WorkOrderColumn.Status,
  WorkOrderColumn.Priority,
  WorkOrderColumn.Category,
  WorkOrderColumn.Assignee,
  WorkOrderColumn.Machine,
  WorkOrderColumn.PlannedStart,
];

export const COLUMN_LABEL_KEYS: Readonly<Record<WorkOrderColumn, string>> = {
  [WorkOrderColumn.Title]: 'WORK_ORDERS.COLUMNS.TITLE',
  [WorkOrderColumn.Status]: 'WORK_ORDERS.COLUMNS.STATUS',
  [WorkOrderColumn.Priority]: 'WORK_ORDERS.COLUMNS.PRIORITY',
  [WorkOrderColumn.Category]: 'WORK_ORDERS.COLUMNS.CATEGORY',
  [WorkOrderColumn.Assignee]: 'WORK_ORDERS.COLUMNS.ASSIGNEE',
  [WorkOrderColumn.Machine]: 'WORK_ORDERS.COLUMNS.MACHINE',
  [WorkOrderColumn.PlannedStart]: 'WORK_ORDERS.COLUMNS.PLANNED_START',
  [WorkOrderColumn.Estimate]: 'WORK_ORDERS.COLUMNS.ESTIMATE',
  [WorkOrderColumn.CreatedAt]: 'WORK_ORDERS.COLUMNS.CREATED_AT',
};

export const STATUS_LABEL_KEYS: Readonly<Record<WorkOrderStatus, string>> = {
  [WorkOrderStatus.Backlog]: 'WORK_ORDERS.STATUSES.BACKLOG',
  [WorkOrderStatus.Staged]: 'WORK_ORDERS.STATUSES.STAGED',
  [WorkOrderStatus.InProgress]: 'WORK_ORDERS.STATUSES.IN_PROGRESS',
  [WorkOrderStatus.Blocked]: 'WORK_ORDERS.STATUSES.BLOCKED',
  [WorkOrderStatus.Completed]: 'WORK_ORDERS.STATUSES.COMPLETED',
  [WorkOrderStatus.Rejected]: 'WORK_ORDERS.STATUSES.REJECTED',
};

export const PRIORITY_LABEL_KEYS: Readonly<Record<WorkOrderPriority, string>> = {
  [WorkOrderPriority.Low]: 'WORK_ORDERS.PRIORITIES.LOW',
  [WorkOrderPriority.Medium]: 'WORK_ORDERS.PRIORITIES.MEDIUM',
  [WorkOrderPriority.High]: 'WORK_ORDERS.PRIORITIES.HIGH',
  [WorkOrderPriority.Critical]: 'WORK_ORDERS.PRIORITIES.CRITICAL',
};

export const CATEGORY_LABEL_KEYS: Readonly<Record<WorkOrderCategory, string>> = {
  [WorkOrderCategory.Inspection]: 'WORK_ORDERS.CATEGORIES.INSPECTION',
  [WorkOrderCategory.Preventive]: 'WORK_ORDERS.CATEGORIES.PREVENTIVE',
  [WorkOrderCategory.Repair]: 'WORK_ORDERS.CATEGORIES.REPAIR',
  [WorkOrderCategory.Other]: 'WORK_ORDERS.CATEGORIES.OTHER',
};

export const MENU_KEYS = {
  open: 'WORK_ORDERS.MENU.OPEN',
  assign: 'WORK_ORDERS.MENU.ASSIGN',
  move: 'WORK_ORDERS.MENU.MOVE',
  unassign: 'WORK_ORDERS.MENU.UNASSIGN',
  status: 'WORK_ORDERS.MENU.STATUS',
  delete: 'WORK_ORDERS.MENU.DELETE',
} as const;

export const SORTABLE_COLUMNS: readonly string[] = Object.values(WorkOrderSortColumn);

/** Maps the pending value of one column filter of the table to the store filter. A missing value means "empty". */
export const FILTER_READERS: Readonly<Record<FilterableColumn, (value: unknown) => Partial<WorkOrderFilter>>> = {
  [WorkOrderColumn.Title]: (value) => ({ title: asText(value) }),
  [WorkOrderColumn.Status]: (value) => ({ statuses: asList<WorkOrderStatus>(value) }),
  [WorkOrderColumn.Priority]: (value) => ({ priorities: asList<WorkOrderPriority>(value) }),
  [WorkOrderColumn.Category]: (value) => ({ categories: asList<WorkOrderCategory>(value) }),
  [WorkOrderColumn.Assignee]: (value) => ({ assigneeIds: asList<string>(value) }),
  [WorkOrderColumn.Machine]: (value) => ({ machineIds: asList<string>(value) }),
  [WorkOrderColumn.PlannedStart]: (value) => ({ plannedStart: asRange(value) }),
};

/** Reads the applied store value of one column as a table filter value. `null` means "no filter". */
export const FILTER_WRITERS: Readonly<Record<FilterableColumn, (filter: WorkOrderFilter) => unknown>> = {
  [WorkOrderColumn.Title]: (filter) => filter.title || null,
  [WorkOrderColumn.Status]: (filter) => (filter.statuses.length > 0 ? [...filter.statuses] : null),
  [WorkOrderColumn.Priority]: (filter) => (filter.priorities.length > 0 ? [...filter.priorities] : null),
  [WorkOrderColumn.Category]: (filter) => (filter.categories.length > 0 ? [...filter.categories] : null),
  [WorkOrderColumn.Assignee]: (filter) => (filter.assigneeIds.length > 0 ? [...filter.assigneeIds] : null),
  [WorkOrderColumn.Machine]: (filter) => (filter.machineIds.length > 0 ? [...filter.machineIds] : null),
  [WorkOrderColumn.PlannedStart]: (filter) => (isRangeEmpty(filter.plannedStart) ? null : filter.plannedStart),
};
