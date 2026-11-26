import type { Provider } from '@angular/core';
import { vi } from 'vitest';

import { AuthStore } from '@shared/security/auth-store/auth.store';

type StubbedMember =
  | 'accessToken'
  | 'currentToken'
  | 'email'
  | 'ensureValidFor'
  | 'forceRefresh'
  | 'hasPermission'
  | 'isAuthenticated'
  | 'login'
  | 'logout'
  | 'setUiLocale'
  | 'signInLocale'
  | 'subject'
  | 'tokenExpiresAt';

/**
 * The `AuthStore` members that a spec may stub. When one of them is renamed, the set becomes empty and every spec that
 * stubs a member fails to build.
 */
export type FakeAuthStoreOverrides = Partial<
  Record<StubbedMember extends keyof InstanceType<typeof AuthStore> ? StubbedMember : never, unknown>
>;

export interface FakeAuthStoreOptions extends FakeAuthStoreOverrides {
  /** The permissions that `hasPermission` grants. A `hasPermission` override replaces it. */
  readonly permissions?: readonly string[];
}

/**
 * Provides a complete fake `AuthStore`. It holds every member that root services (such as `LanguageStore`) read, so a
 * root effect never meets a missing member. A spec passes the permissions to grant (none by default) and only the
 * members that it needs.
 */
export function provideFakeAuthStore({ permissions = [], ...overrides }: FakeAuthStoreOptions = {}): Provider {
  return {
    provide: AuthStore,
    useValue: {
      hasPermission: (permission: string) => permissions.includes(permission),
      setUiLocale: vi.fn(),
      signInLocale: () => null,
      ensureValidFor: () => Promise.resolve(),
      ...overrides,
    },
  };
}
