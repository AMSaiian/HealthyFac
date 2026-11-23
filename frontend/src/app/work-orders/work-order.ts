import type { DateRangeBody, IdSetBody, SetFilterBody, TextFilterBody } from '@shared/grid/requests/grid-filters';
import type { GridPage, GridQueryState, GridRequestBody, GridSort } from '@shared/grid/requests/grid-request';
import type { PlainDateRange } from '@shared/time/plain-date-range';

export enum WorkOrderStatus {
  Backlog = 'backlog',
  Staged = 'staged',
  InProgress = 'inProgress',
  Blocked = 'blocked',
  Completed = 'completed',
  Rejected = 'rejected',
}

export enum WorkOrderPriority {
  Low = 'low',
  Medium = 'medium',
  High = 'high',
  Critical = 'critical',
}

export enum WorkOrderCategory {
  Inspection = 'inspection',
  Preventive = 'preventive',
  Repair = 'repair',
  Other = 'other',
}

export enum WorkOrderColumn {
  Title = 'title',
  Status = 'status',
  Priority = 'priority',
  Category = 'category',
  Assignee = 'assignee',
  Machine = 'machine',
  PlannedStart = 'plannedStart',
  Estimate = 'estimate',
  CreatedAt = 'createdAt',
}

/** The columns that the backend sorts by. */
export enum WorkOrderSortColumn {
  Title = 'title',
  Priority = 'priority',
  PlannedStart = 'plannedStart',
  CreatedAt = 'createdAt',
}

export interface WorkOrder {
  readonly id: string;
  readonly title: string;
  readonly description: string | null;
  readonly priority: WorkOrderPriority;
  readonly category: WorkOrderCategory;
  readonly estimateMinutes: number;
  readonly status: WorkOrderStatus;
  readonly assigneeId: string | null;
  readonly assigneeName: string | null;
  readonly plannedStartLocal: string | null;
  readonly plannedStartUtc: string | null;
  readonly isDelayed: boolean;
  readonly machineId: string | null;
  readonly machineName: string | null;
  readonly isMachineDeleted: boolean;
  readonly fileIds: readonly string[];
  readonly rowVersion: number;
  readonly createdAt: string;
}

export interface WorkOrderRow {
  readonly id: string;
  readonly title: string;
  readonly status: WorkOrderStatus;
  readonly priority: WorkOrderPriority;
  readonly category: WorkOrderCategory;
  readonly assigneeId: string | null;
  readonly assigneeName: string | null;
  readonly machineId: string | null;
  readonly machineName: string | null;
  readonly isMachineDeleted: boolean;
  readonly plannedStartLocal: string | null;
  readonly plannedStartUtc: string | null;
  readonly estimateMinutes: number;
  readonly isDelayed: boolean;
  readonly createdAt: string;
  readonly rowVersion: number;
}

export interface WorkOrderHistoryRow {
  readonly revision: number;
  readonly status: WorkOrderStatus;
  readonly assigneeId: string | null;
  readonly assigneeName: string | null;
  readonly plannedStartLocal: string | null;
  readonly plannedStartUtc: string | null;
  readonly estimateMinutes: number;
  readonly createdAt: string;
  readonly createdBy: string;
}

/** One page of the history, newest first. `nextAfter` is the revision to send for the next page, or `null`. */
export interface WorkOrderHistoryPage {
  readonly items: readonly WorkOrderHistoryRow[];
  readonly totalCount: number;
  readonly nextAfter: number | null;
}

/** The fields that the form edits. The file IDs are the encrypted IDs of the Document service. */
export interface WorkOrderBody {
  readonly title: string;
  readonly description: string | null;
  readonly priority: WorkOrderPriority;
  readonly category: WorkOrderCategory;
  readonly machineId: string | null;
  readonly estimateMinutes: number;
  readonly fileIds: readonly string[];
}

export interface CreateWorkOrderBody extends WorkOrderBody {
  readonly assigneeId?: string;
  readonly plannedStartLocal?: string;
}

export interface UpdateWorkOrderBody extends WorkOrderBody {
  readonly rowVersion: number;
  readonly assigneeId: string | null;
  readonly plannedStartLocal: string | null;
}

export interface CreateWorkOrderResponse {
  readonly id: string;
}

export interface ChangeStatusBody {
  readonly status: WorkOrderStatus;
  readonly rowVersion: number;
}

export interface AssignBody {
  readonly assigneeId: string;
  readonly plannedStartLocal: string;
  readonly rowVersion: number;
}

export interface MoveBody {
  readonly assigneeId?: string;
  readonly plannedStartLocal: string;
  readonly rowVersion: number;
}

/** A file of the Document service as the lookup returns it. */
export interface LookedUpFile {
  readonly id: string;
  readonly fileName: string;
  readonly extension: string;
  readonly contentType: string;
  readonly sizeBytes: number;
  readonly machineId: string | null;
}

export interface LookupFilesResponse {
  readonly files: readonly LookedUpFile[];
}

export interface DayScheduleBlock {
  readonly startLocal: string;
  readonly endLocal: string;
  readonly startUtc: string;
  readonly endUtc: string;
}

/** The source of the blocks of a day: the weekly template, an override of the date, or no shift. */
export enum DayScheduleSource {
  Template = 'template',
  Override = 'override',
  None = 'none',
}

export interface DayScheduleWorkOrder {
  readonly id: string;
  readonly title: string;
  readonly status: WorkOrderStatus;
  readonly priority: WorkOrderPriority;
  readonly plannedStartLocal: string;
  readonly plannedStartUtc: string;
  readonly estimateMinutes: number;
  readonly occupancyEndUtc: string;
  readonly isDelayed: boolean;
  readonly rowVersion: number;
}

/** One day of the day summary: whether the employee works, and the longest free span in minutes. */
export interface ScheduleDay {
  readonly date: string;
  readonly hasBlocks: boolean;
  readonly longestFreeMinutes: number;
}

/** The answer of `GET /employees/{id}/schedule/days`. */
export interface ScheduleDays {
  readonly timeZoneId: string;
  readonly days: readonly ScheduleDay[];
}

/** One free span of a day, local and UTC. */
export interface FreeSpan {
  readonly startLocal: string;
  readonly endLocal: string;
  readonly startUtc: string;
  readonly endUtc: string;
}

/** The answer of `GET /employees/{id}/schedule/free-spans`. */
export interface FreeSpans {
  readonly date: string;
  readonly timeZoneId: string;
  readonly spans: readonly FreeSpan[];
}

/** The day of one employee as `GET /employees/{id}/schedule` returns it. */
export interface DaySchedule {
  readonly date: string;
  readonly timeZoneId: string;
  readonly source: DayScheduleSource;
  readonly blocks: readonly DayScheduleBlock[];
  readonly workOrders: readonly DayScheduleWorkOrder[];
}

export type WorkOrdersPage = GridPage<WorkOrderRow>;

export type WorkOrderSort = GridSort<WorkOrderSortColumn>;

export interface WorkOrderFilter {
  readonly title: string;
  readonly statuses: readonly WorkOrderStatus[];
  readonly priorities: readonly WorkOrderPriority[];
  readonly categories: readonly WorkOrderCategory[];
  readonly assigneeIds: readonly string[];
  readonly machineIds: readonly string[];
  readonly plannedStart: PlainDateRange;
}

export type WorkOrderGridState = GridQueryState<WorkOrderFilter, WorkOrderSortColumn>;

export interface WorkOrderFilterBody {
  readonly title?: TextFilterBody;
  readonly statuses?: SetFilterBody<WorkOrderStatus>;
  readonly priorities?: SetFilterBody<WorkOrderPriority>;
  readonly categories?: SetFilterBody<WorkOrderCategory>;
  readonly assignee?: IdSetBody;
  readonly machine?: IdSetBody;
  readonly plannedStart?: DateRangeBody;
}

export type SearchWorkOrdersBody = GridRequestBody<WorkOrderFilterBody, WorkOrderSortColumn>;

/** One change of the schedule tab: the target state of a work order. */
export interface ScheduleChangeBody {
  readonly workOrderId: string;
  readonly rowVersion: number;
  readonly assigneeId: string | null;
  readonly plannedStartLocal: string | null;
  readonly estimateMinutes: number;
}

export interface ApplyScheduleBody {
  readonly changes: readonly ScheduleChangeBody[];
}

export interface ApplyScheduleResponse {
  readonly workOrders: readonly { readonly id: string; readonly rowVersion: number }[];
}

/** The reasons of a rejected change, as the `422` carries them in `changes[].reason`. */
export enum ScheduleChangeRejection {
  NotEditable = 'notEditable',
  AssigneeNotActive = 'assigneeNotActive',
  StartOutOfRange = 'startOutOfRange',
  OutsideShift = 'outsideShift',
  SlotTaken = 'slotTaken',
}
