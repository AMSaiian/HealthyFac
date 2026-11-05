import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { patchState, signalStore, withMethods, withProps, withState } from '@ngrx/signals';

import { KEYCLOAK } from './keycloak';
import { type Permission, readPermissions } from './permissions';
import { isSessionEndedByServer, SessionEndedError } from './session-errors';

const MIN_TOKEN_VALIDITY_SECONDS = 30;
const FORCE_REFRESH_VALIDITY_SECONDS = -1;
const SILENT_CHECK_SSO_PATH = '/silent-check-sso.html';
const NO_SESSION_MESSAGE = 'The session has ended.';

interface PendingOperations {
  login: Promise<void> | null;
  refresh: Promise<void> | null;
}

const createPendingOperations = (): PendingOperations => ({ login: null, refresh: null });

export const AuthStore = signalStore(
  { providedIn: 'root' },
  withState({ isAuthenticated: false, permissions: [] as readonly string[] }),
  withProps(() => ({
    _keycloak: inject(KEYCLOAK),
    _router: inject(Router),
    _pending: createPendingOperations(),
  })),
  withMethods((store) => {
    const login = (redirectPath: string): Promise<void> => {
      store._pending.login ??= store._keycloak
        .login({ redirectUri: `${location.origin}${redirectPath}` })
        .catch((error: unknown) => {
          store._pending.login = null;
          console.error(error);
        });
      return store._pending.login;
    };

    const hasSession = (): boolean =>
      store._keycloak.refreshToken !== undefined && store._keycloak.tokenParsed !== undefined;

    const runRefresh = async (): Promise<void> => {
      if (!hasSession()) {
        throw new SessionEndedError(NO_SESSION_MESSAGE);
      }
      try {
        await store._keycloak.updateToken(FORCE_REFRESH_VALIDITY_SECONDS);
      } catch (error: unknown) {
        if (isSessionEndedByServer(error)) {
          throw new SessionEndedError(NO_SESSION_MESSAGE, { cause: error });
        }
        throw error;
      }
    };

    const refresh = (): Promise<void> => {
      store._pending.refresh ??= runRefresh().finally(() => {
        store._pending.refresh = null;
      });
      return store._pending.refresh;
    };

    return {
      async init(): Promise<void> {
        store._keycloak.onAuthRefreshSuccess = (): void => {
          patchState(store, { permissions: readPermissions(store._keycloak.idTokenParsed) });
        };
        store._keycloak.onTokenExpired = (): void => {
          refresh().catch((error: unknown) =>
            error instanceof SessionEndedError ? login(store._router.url) : undefined,
          );
        };
        const isAuthenticated = await store._keycloak.init({
          onLoad: 'check-sso',
          silentCheckSsoRedirectUri: `${location.origin}${SILENT_CHECK_SSO_PATH}`,
          pkceMethod: 'S256',
          checkLoginIframe: false,
        });
        patchState(store, { isAuthenticated, permissions: readPermissions(store._keycloak.idTokenParsed) });
      },
      login,
      hasPermission(permission: Permission): boolean {
        return store.permissions().includes(permission);
      },
      logout(): Promise<void> {
        return store._keycloak.logout({ redirectUri: `${location.origin}/` });
      },
      async accessToken(): Promise<string> {
        if (!hasSession()) {
          throw new SessionEndedError(NO_SESSION_MESSAGE);
        }
        if (store._keycloak.isTokenExpired(MIN_TOKEN_VALIDITY_SECONDS)) {
          await refresh();
        }
        if (store._keycloak.token === undefined) {
          throw new SessionEndedError(NO_SESSION_MESSAGE);
        }
        return store._keycloak.token;
      },
      currentToken(): string | undefined {
        return store._keycloak.token;
      },
      forceRefresh: refresh,
    };
  }),
);
