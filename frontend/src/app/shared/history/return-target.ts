import type { Permission } from '@shared/security/permissions';

/** The page that a Back button goes to when the app has no previous entry, and the permission that page needs. */
export interface ReturnTarget {
  readonly url: string;
  readonly permission?: Permission;
}

export const HOME_RETURN_TARGET = { url: '/' } as const satisfies ReturnTarget;
