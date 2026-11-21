import type { ReturnTarget } from '@shared/history/return-target';
import { Permission } from '@shared/security/permissions';

export const EMPLOYEES_RETURN_TARGET = {
  url: '/employees',
  permission: Permission.EmployeesRead,
} as const satisfies ReturnTarget;
