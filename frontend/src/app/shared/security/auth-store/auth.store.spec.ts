import { HttpStatusCode } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { AuthStore } from './auth.store';
import { KEYCLOAK } from '@shared/security/keycloak';
import { Permission } from '@shared/security/permissions';
import { SessionEndedError } from '@shared/security/session-errors';
import { asKeycloak, createFakeKeycloak, type FakeKeycloak } from '@shared/security/testing/fake-keycloak';

function networkError(status: number): Error {
  return Object.assign(new Error('refresh failed'), { response: new Response(null, { status }) });
}

function deferred(): { promise: Promise<boolean>; resolve: () => void } {
  let resolve!: () => void;
  const promise = new Promise<boolean>((done) => {
    resolve = (): void => {
      done(true);
    };
  });
  return { promise, resolve };
}

describe('AuthStore', () => {
  let keycloak: FakeKeycloak;

  function createStore(): InstanceType<typeof AuthStore> {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: KEYCLOAK, useValue: asKeycloak(keycloak) }],
    });
    return TestBed.inject(AuthStore);
  }

  beforeEach(() => {
    keycloak = createFakeKeycloak();
  });

  it('login_CalledThreeTimes_CallsKeycloakLoginOnce', async () => {
    const store = createStore();

    await Promise.all([store.login('/some/path?x=1'), store.login('/other'), store.login('/')]);

    expect(keycloak.login).toHaveBeenCalledTimes(1);
    expect(keycloak.login).toHaveBeenCalledWith({ redirectUri: `${location.origin}/some/path?x=1` });
  });

  it('login_AfterRejectedLogin_CallsKeycloakLoginAgain', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    keycloak.login.mockRejectedValueOnce(new Error('login failed'));
    const store = createStore();

    await store.login('/first');
    await store.login('/second');

    expect(keycloak.login).toHaveBeenCalledTimes(2);
  });

  it('login_WhenKeycloakLoginRejects_ResolvesAndLogs', async () => {
    const failure = new Error('login failed');
    const logged = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    keycloak.login.mockRejectedValue(failure);
    const store = createStore();

    const result = store.login('/');

    await expect(result).resolves.toBeUndefined();
    expect(logged).toHaveBeenCalledWith(failure);
  });

  it('init_Always_SetsTokenExpiredHandlerBeforeKeycloakInit', async () => {
    let isHandlerSetDuringInit = false;
    keycloak.init.mockImplementation(() => {
      isHandlerSetDuringInit = typeof keycloak.onTokenExpired === 'function';
      return Promise.resolve(false);
    });
    const store = createStore();

    await store.init();

    expect(isHandlerSetDuringInit).toBe(true);
  });

  it('init_WithSession_SetsIsAuthenticated', async () => {
    keycloak.init.mockResolvedValue(true);
    const store = createStore();

    await store.init();

    expect(store.isAuthenticated()).toBe(true);
    expect(keycloak.init).toHaveBeenCalledWith({
      onLoad: 'check-sso',
      silentCheckSsoRedirectUri: `${location.origin}/silent-check-sso.html`,
      pkceMethod: 'S256',
      checkLoginIframe: false,
    });
  });

  it('init_WithoutSession_KeepsIsAuthenticatedFalse', async () => {
    const store = createStore();

    await store.init();

    expect(store.isAuthenticated()).toBe(false);
  });

  it('login_WithUiLocale_PassesTheLocaleToKeycloak', async () => {
    const store = createStore();
    store.setUiLocale('uk');

    await store.login('/path');

    expect(keycloak.login).toHaveBeenCalledWith({ redirectUri: `${location.origin}/path`, locale: 'uk' });
  });

  describe('sign-in locale', () => {
    afterEach(() => {
      history.replaceState(null, '', '/');
    });

    it('init_AfterLoginRedirectWithLocaleClaim_ExposesTheLocale', async () => {
      history.replaceState(null, '', '/#state=abc&session_state=def&code=ghi');
      keycloak.init.mockResolvedValue(true);
      keycloak.idTokenParsed = { locale: 'uk' };
      const store = createStore();

      await store.init();

      expect(store.signInLocale()).toBe('uk');
    });

    it('init_WithSilentSsoWithoutRedirectReturn_ExposesNoLocale', async () => {
      keycloak.init.mockResolvedValue(true);
      keycloak.idTokenParsed = { locale: 'uk' };
      const store = createStore();

      await store.init();

      expect(store.signInLocale()).toBeNull();
    });

    it('onAuthRefreshSuccess_WithNewLocaleClaim_KeepsTheSignInLocale', async () => {
      keycloak.init.mockResolvedValue(true);
      const store = createStore();
      await store.init();
      keycloak.idTokenParsed = { locale: 'uk' };

      keycloak.onAuthRefreshSuccess?.();

      expect(store.signInLocale()).toBeNull();
    });
  });

  it('forceRefresh_CalledThreeTimesAtOnce_CallsUpdateTokenOnce', async () => {
    const running = deferred();
    keycloak.updateToken.mockReturnValue(running.promise);
    const store = createStore();

    const all = Promise.all([store.forceRefresh(), store.forceRefresh(), store.forceRefresh()]);
    running.resolve();
    await all;

    expect(keycloak.updateToken).toHaveBeenCalledTimes(1);
    expect(keycloak.updateToken).toHaveBeenCalledWith(-1);
  });

  it('forceRefresh_AfterPreviousRefreshSettled_CallsUpdateTokenAgain', async () => {
    const store = createStore();

    await store.forceRefresh();
    await store.forceRefresh();

    expect(keycloak.updateToken).toHaveBeenCalledTimes(2);
  });

  it('forceRefresh_With400Failure_ThrowsSessionEndedError', async () => {
    const failure = networkError(HttpStatusCode.BadRequest);
    keycloak.updateToken.mockRejectedValue(failure);
    const store = createStore();

    const result = store.forceRefresh();

    await expect(result).rejects.toBeInstanceOf(SessionEndedError);
    await expect(result).rejects.toHaveProperty('cause', failure);
  });

  it('forceRefresh_WithTransientFailure_RethrowsOriginalError', async () => {
    const failure = new TypeError('Failed to fetch');
    keycloak.updateToken.mockRejectedValue(failure);
    const store = createStore();

    const result = store.forceRefresh();

    await expect(result).rejects.toBe(failure);
  });

  it('forceRefresh_WithoutSession_ThrowsSessionEndedErrorWithoutUpdateToken', async () => {
    keycloak.refreshToken = undefined;
    const store = createStore();

    const result = store.forceRefresh();

    await expect(result).rejects.toBeInstanceOf(SessionEndedError);
    expect(keycloak.updateToken).not.toHaveBeenCalled();
  });

  it('accessToken_WithValidToken_DoesNotRefresh', async () => {
    const store = createStore();

    const token = await store.accessToken();

    expect(token).toBe('token-1');
    expect(keycloak.updateToken).not.toHaveBeenCalled();
  });

  it('accessToken_WithExpiringTokenFromThreeCallers_RefreshesOnce', async () => {
    const running = deferred();
    keycloak.isTokenExpired.mockReturnValue(true);
    keycloak.updateToken.mockReturnValue(running.promise);
    const store = createStore();

    const all = Promise.all([store.accessToken(), store.accessToken(), store.accessToken()]);
    running.resolve();
    const tokens = await all;

    expect(keycloak.updateToken).toHaveBeenCalledTimes(1);
    expect(tokens).toEqual(['token-1', 'token-1', 'token-1']);
  });

  it('accessToken_WithoutSession_ThrowsSessionEndedErrorWithoutRefresh', async () => {
    keycloak.tokenParsed = undefined;
    const store = createStore();

    const result = store.accessToken();

    await expect(result).rejects.toBeInstanceOf(SessionEndedError);
    expect(keycloak.updateToken).not.toHaveBeenCalled();
  });

  it('onTokenExpired_WithSessionEnded_CallsLogin', async () => {
    keycloak.updateToken.mockRejectedValue(networkError(HttpStatusCode.BadRequest));
    const store = createStore();
    const router = TestBed.inject(Router);
    await store.init();

    keycloak.onTokenExpired?.();

    await vi.waitFor(() => {
      expect(keycloak.login).toHaveBeenCalledWith({ redirectUri: `${location.origin}${router.url}` });
    });
  });

  it('onTokenExpired_WithTransientFailure_DoesNotCallLogin', async () => {
    keycloak.updateToken.mockRejectedValue(new TypeError('Failed to fetch'));
    const store = createStore();
    await store.init();

    keycloak.onTokenExpired?.();

    await vi.waitFor(() => {
      expect(keycloak.updateToken).toHaveBeenCalled();
    });
    await new Promise((resolve) => setTimeout(resolve));
    expect(keycloak.login).not.toHaveBeenCalled();
  });

  it('onTokenExpired_WithSuccessfulRefresh_DoesNotCallLogin', async () => {
    const store = createStore();
    await store.init();

    keycloak.onTokenExpired?.();

    await vi.waitFor(() => {
      expect(keycloak.updateToken).toHaveBeenCalled();
    });
    expect(keycloak.login).not.toHaveBeenCalled();
  });

  it('init_WithPermissionsClaim_ExposesPermissions', async () => {
    keycloak.init.mockResolvedValue(true);
    keycloak.idTokenParsed = { permissions: [Permission.MachinesRead, Permission.MachinesWrite] };
    const store = createStore();

    await store.init();

    expect(store.permissions()).toEqual([Permission.MachinesRead, Permission.MachinesWrite]);
    expect(store.hasPermission(Permission.MachinesRead)).toBe(true);
    expect(store.hasPermission(Permission.MachinesDelete)).toBe(false);
  });

  it('refresh_WithNewPermissions_UpdatesPermissions', async () => {
    keycloak.init.mockResolvedValue(true);
    keycloak.idTokenParsed = { permissions: [Permission.MachinesRead] };
    const store = createStore();
    await store.init();

    keycloak.idTokenParsed = { permissions: [Permission.MachinesRead, Permission.MachinesDelete] };
    keycloak.onAuthRefreshSuccess?.();

    expect(store.permissions()).toEqual([Permission.MachinesRead, Permission.MachinesDelete]);
  });

  it('hasPermission_WithoutClaim_ReturnsFalse', async () => {
    keycloak.init.mockResolvedValue(true);
    keycloak.idTokenParsed = {};
    const store = createStore();
    await store.init();

    const result = store.hasPermission(Permission.MachinesRead);

    expect(result).toBe(false);
  });

  it('init_WithMalformedPermissionsClaim_KeepsOnlyTextValues', async () => {
    keycloak.init.mockResolvedValue(true);
    keycloak.idTokenParsed = { permissions: [Permission.MachinesRead, 42, null] };
    const store = createStore();

    await store.init();

    expect(store.permissions()).toEqual([Permission.MachinesRead]);
  });
});
