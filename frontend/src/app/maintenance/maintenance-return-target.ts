import type { ReturnTarget } from '@shared/history/return-target';
import { Permission } from '@shared/security/permissions';

export const CASES_RETURN_TARGET = {
  url: '/maintenance/cases',
  permission: Permission.CasesRead,
} as const satisfies ReturnTarget;
