import type { AppAction } from '@shared/actions/app-action';
import { Temporal } from '@shared/time/temporal';

import type { FilesScope } from '@files/file';
import {
  type CreateWorkOrderBody,
  type LookedUpFile,
  type UpdateWorkOrderBody,
  type WorkOrder,
  WorkOrderCategory,
  WorkOrderPriority,
} from '@work-orders/work-order';
import type { WorkOrderActionPayload, WorkOrderFormValue } from '@work-orders/work-order-schema';

export const scopeOf = (machineId: string | null): FilesScope =>
  machineId === null ? { kind: 'company' } : { kind: 'machine', machineId };

const DEFAULT_ESTIMATE_MINUTES = 60;

export const EMPTY_VALUE: WorkOrderFormValue = {
  title: '',
  description: null,
  priority: WorkOrderPriority.Medium,
  category: WorkOrderCategory.Other,
  machineId: null,
  estimateMinutes: DEFAULT_ESTIMATE_MINUTES,
  fileIds: [],
  assigneeId: null,
  start: null,
};

export interface FileEntry {
  readonly id: string;
  readonly file: LookedUpFile | null;
}

export function toFormValue(workOrder: WorkOrder): WorkOrderFormValue {
  return {
    title: workOrder.title,
    description: workOrder.description,
    priority: workOrder.priority,
    category: workOrder.category,
    machineId: workOrder.machineId,
    estimateMinutes: workOrder.estimateMinutes,
    fileIds: [...workOrder.fileIds],
    assigneeId: workOrder.assigneeId,
    start: workOrder.plannedStartLocal === null ? null : Temporal.PlainDateTime.from(workOrder.plannedStartLocal),
  };
}

export function payloadOf(action: AppAction | null): WorkOrderActionPayload {
  return (action?.payload ?? {}) as WorkOrderActionPayload;
}

export function overlay(base: WorkOrderFormValue, payload: WorkOrderActionPayload): WorkOrderFormValue {
  return {
    title: payload.title ?? base.title,
    description: payload.description === undefined ? base.description : payload.description,
    priority: payload.priority ?? base.priority,
    category: payload.category ?? base.category,
    machineId: payload.machineId === undefined ? base.machineId : payload.machineId,
    estimateMinutes: payload.estimateMinutes ?? base.estimateMinutes,
    fileIds: payload.fileIds ?? base.fileIds,
    assigneeId: payload.assigneeId === undefined ? base.assigneeId : payload.assigneeId,
    start: payload.start === undefined ? base.start : payload.start,
  };
}

export function toCreateBody(value: WorkOrderFormValue): CreateWorkOrderBody {
  return {
    title: value.title.trim(),
    description: value.description,
    priority: value.priority,
    category: value.category,
    machineId: value.machineId,
    estimateMinutes: value.estimateMinutes,
    fileIds: value.fileIds,
    ...(value.assigneeId !== null && value.start !== null
      ? { assigneeId: value.assigneeId, plannedStartLocal: value.start.toString() }
      : {}),
  };
}

/**
 * Builds the update body. A user without `work-orders:manage` may not change the estimate or the slot (the backend
 * answers `403`), so the body then carries the stored values and never the edited ones. The body always carries the
 * assignee and the start: both `null` unassigns the work order.
 */
export function toUpdateBody(
  value: WorkOrderFormValue,
  workOrder: Pick<WorkOrder, 'estimateMinutes' | 'rowVersion' | 'assigneeId' | 'plannedStartLocal'>,
  canManage: boolean,
): UpdateWorkOrderBody {
  const { assigneeId, plannedStartLocal, ...body } = toCreateBody(value);
  return {
    ...body,
    estimateMinutes: canManage ? value.estimateMinutes : workOrder.estimateMinutes,
    rowVersion: workOrder.rowVersion,
    assigneeId: canManage ? (assigneeId ?? null) : workOrder.assigneeId,
    plannedStartLocal: canManage ? (plannedStartLocal ?? null) : workOrder.plannedStartLocal,
  };
}

/** Pairs each attached file ID with its lookup entry. A file that the lookup does not return stays in the list. */
export function fileEntries(fileIds: readonly string[], lookup: readonly LookedUpFile[]): FileEntry[] {
  const byId = new Map(lookup.map((file) => [file.id, file]));
  return fileIds.map((id) => ({ id, file: byId.get(id) ?? null }));
}
