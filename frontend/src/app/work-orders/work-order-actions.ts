import { type infer as Infer, number, object, string } from 'zod/mini';

import type { ComposeCodec } from '@shared/actions/app-action';
import { hasOnlyStringKeys } from '@shared/actions/compose-codec';
import { formatCompose } from '@shared/history/history.store.utils';
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

export const WORK_ORDER_COMPOSE_NAME = 'work-order';

/** `work-order` opens a new work order. `work-order:<id>` opens a stored one. A prefilled new work order stays in memory. */
export const WORK_ORDER_COMPOSE: ComposeCodec = {
  name: WORK_ORDER_COMPOSE_NAME,
  encode: (action) =>
    action.type === WorkOrderActionType.Open && hasOnlyStringKeys(action.payload, ['workOrderId'])
      ? formatCompose(WORK_ORDER_COMPOSE_NAME, action.payload['workOrderId'] ?? null)
      : null,
  decode: (id) => ({ type: WorkOrderActionType.Open, payload: id === null ? {} : { workOrderId: id } }),
};
