import { type infer as Infer, number, object, string } from 'zod/mini';

import type { WorkOrderStatus } from '@work-orders/work-order';

export const WorkOrderActionType = {
  Open: 'work-orders.open',
  Saved: 'work-orders.saved',
  Assign: 'work-orders.assign',
  Move: 'work-orders.move',
  Unassign: 'work-orders.unassign',
  ChangeStatus: 'work-orders.status',
  Delete: 'work-orders.delete',
  Conflict: 'work-orders.conflict',
} as const;

/** The payload of a row action that a schedule action host runs: the row and the row version that the user saw. */
export const workOrderRowActionPayloadSchema = object({ workOrderId: string(), rowVersion: number() });

/** The detail of a `Saved` result after a create: the status in which the new work order landed. */
export interface SavedDetail {
  readonly status: WorkOrderStatus;
}

export type WorkOrderRowActionPayload = Infer<typeof workOrderRowActionPayloadSchema>;
