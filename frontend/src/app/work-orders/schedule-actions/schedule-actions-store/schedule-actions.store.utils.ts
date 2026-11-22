import type { AppAction } from '@shared/actions/app-action';

import { WorkOrderStatus } from '@work-orders/work-order';
import type { WorkOrderRowActionPayload } from '@work-orders/work-order-actions';

export const CHANGEABLE_STATUSES: readonly WorkOrderStatus[] = [
  WorkOrderStatus.Staged,
  WorkOrderStatus.InProgress,
  WorkOrderStatus.Blocked,
  WorkOrderStatus.Completed,
  WorkOrderStatus.Rejected,
];

export const payloadOf = (action: AppAction | null): Partial<WorkOrderRowActionPayload> =>
  (action?.payload ?? {}) as Partial<WorkOrderRowActionPayload>;
