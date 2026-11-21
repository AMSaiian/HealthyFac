import type { ReturnTarget } from '@shared/history/return-target';
import { Permission } from '@shared/security/permissions';

export const WORK_ORDERS_RETURN_TARGET = {
  url: '/work-orders',
  permission: Permission.WorkOrdersRead,
} as const satisfies ReturnTarget;
