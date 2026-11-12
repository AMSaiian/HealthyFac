import {
  HttpClient,
  HttpContext,
  HttpErrorResponse,
  HttpStatusCode,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { APP_CONFIG } from '@shared/config/app-config';
import { ANONYMOUS_REQUEST } from '@shared/http/anonymous-request';

import { authInterceptor } from './auth.interceptor';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { SessionEndedError } from './session-errors';

const API = 'https://api.healthyfac.test';
const API_URL = `${API}/api/core/v1/things`;
const UNAUTHORIZED = { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' };

interface FakeStore {
  accessToken: ReturnType<typeof vi.fn>;
  currentToken: ReturnType<typeof vi.fn>;
  forceRefresh: ReturnType<typeof vi.fn>;
  login: ReturnType<typeof vi.fn>;
}

describe('authInterceptor', () => {
  let store: FakeStore;
  let http: HttpClient;
  let controller: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    store = {
      accessToken: vi.fn().mockResolvedValue('token-1'),
      currentToken: vi.fn(),
      forceRefresh: vi.fn().mockResolvedValue(undefined),
      login: vi.fn().mockResolvedValue(undefined),
    };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        {
          provide: APP_CONFIG,
          useValue: {
            apiBaseUrl: API,
            authUrl: 'https://auth.healthyfac.test',
            authRealm: 'r',
            authClientId: 'c',
            primeUiLicenseKey: 'k',
          },
        },
        { provide: AuthStore, useValue: store },
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    controller.verify();
  });

  it('authInterceptor_ForAnonymousApiRequestWithoutSession_SendsItWithoutTokenAndStartsNoLogin', async () => {
    store.accessToken.mockRejectedValue(new SessionEndedError('The session has ended.'));
    const result = firstValueFrom(http.get(API_URL, { context: new HttpContext().set(ANONYMOUS_REQUEST, true) }));

    const request = controller.expectOne(API_URL);
    request.flush({});
    await result;

    expect(request.request.headers.has('Authorization')).toBe(false);
    expect(store.login).not.toHaveBeenCalled();
  });

  it('authInterceptor_ForApiUrl_AddsBearerHeader', async () => {
    const result = firstValueFrom(http.get(API_URL));

    const request = await vi.waitFor(() => controller.expectOne(API_URL));
    request.flush({});
    await result;

    expect(request.request.headers.get('Authorization')).toBe('Bearer token-1');
  });

  it.each(['https://evil.test/api/x', 'https://api.healthyfac.test.evil.test/x', '/i18n/en.json'])(
    'authInterceptor_ForOtherHostOrRelativeUrl_AddsNoHeader (%s)',
    async (url) => {
      const result = firstValueFrom(http.get(url));

      const request = controller.expectOne(url);
      request.flush({});
      await result;

      expect(request.request.headers.has('Authorization')).toBe(false);
      expect(store.accessToken).not.toHaveBeenCalled();
    },
  );

  it('authInterceptor_ForOtherHostWith401_DoesNotRefreshOrLogin', async () => {
    const url = 'https://evil.test/api/x';
    const result = firstValueFrom(http.get(url)).catch((error: unknown) => error);

    controller.expectOne(url).flush({}, UNAUTHORIZED);
    await result;

    expect(store.forceRefresh).not.toHaveBeenCalled();
    expect(store.login).not.toHaveBeenCalled();
  });

  it('authInterceptor_OnFirst401_RefreshesOnceAndRetriesWithNewToken', async () => {
    store.accessToken.mockResolvedValueOnce('old-token').mockResolvedValueOnce('new-token');
    store.currentToken.mockReturnValue('old-token');
    const result = firstValueFrom(http.get(API_URL));

    const first = await vi.waitFor(() => controller.expectOne(API_URL));
    first.flush({}, UNAUTHORIZED);
    const retry = await vi.waitFor(() => controller.expectOne(API_URL));
    retry.flush({ ok: true });
    const body = await result;

    expect(first.request.headers.get('Authorization')).toBe('Bearer old-token');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer new-token');
    expect(store.forceRefresh).toHaveBeenCalledTimes(1);
    expect(store.login).not.toHaveBeenCalled();
    expect(body).toEqual({ ok: true });
  });

  it('authInterceptor_On401WithNewerTokenInStore_RetriesWithoutRefresh', async () => {
    store.accessToken.mockResolvedValueOnce('a').mockResolvedValueOnce('b');
    const result = firstValueFrom(http.get(API_URL));

    const first = await vi.waitFor(() => controller.expectOne(API_URL));
    store.currentToken.mockReturnValue('b');
    first.flush({}, UNAUTHORIZED);
    const retry = await vi.waitFor(() => controller.expectOne(API_URL));
    retry.flush({ ok: true });
    await result;

    expect(first.request.headers.get('Authorization')).toBe('Bearer a');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer b');
    expect(store.forceRefresh).not.toHaveBeenCalled();
    expect(store.login).not.toHaveBeenCalled();
  });

  it('authInterceptor_OnSecond401_CallsLoginAndSendsNoThirdRequest', async () => {
    const result = firstValueFrom(http.get(API_URL)).catch((error: unknown) => error);

    const first = await vi.waitFor(() => controller.expectOne(API_URL));
    first.flush({}, UNAUTHORIZED);
    const retry = await vi.waitFor(() => controller.expectOne(API_URL));
    retry.flush({}, UNAUTHORIZED);
    await result;

    expect(store.forceRefresh).toHaveBeenCalledTimes(1);
    expect(store.login).toHaveBeenCalledTimes(1);
    expect(store.login).toHaveBeenCalledWith(router.url);
    controller.expectNone(API_URL);
  });

  it('authInterceptor_WithSessionEnded_CallsLoginAndRethrows', async () => {
    const refreshError = new SessionEndedError('session ended');
    store.forceRefresh.mockRejectedValue(refreshError);
    const result = firstValueFrom(http.get(API_URL)).catch((error: unknown) => error);

    const first = await vi.waitFor(() => controller.expectOne(API_URL));
    first.flush({}, UNAUTHORIZED);
    const error = await result;

    expect(error).toBe(refreshError);
    expect(store.login).toHaveBeenCalledWith(router.url);
    controller.expectNone(API_URL);
  });

  it('authInterceptor_WithTransientRefreshFailure_ThrowsStatusZeroWithoutLogin', async () => {
    const failure = new TypeError('Failed to fetch');
    store.forceRefresh.mockRejectedValue(failure);
    const result = firstValueFrom(http.get(API_URL)).catch((error: unknown) => error);

    const first = await vi.waitFor(() => controller.expectOne(API_URL));
    first.flush({}, UNAUTHORIZED);
    const error = await result;

    expect(error).toBeInstanceOf(HttpErrorResponse);
    expect(error).toMatchObject({ status: 0, url: API_URL, error: failure });
    expect(store.login).not.toHaveBeenCalled();
    controller.expectNone(API_URL);
  });

  it('authInterceptor_WithFailedTokenBeforeFirstSend_CallsLoginAndSendsNothing', async () => {
    store.accessToken.mockRejectedValue(new SessionEndedError('no session'));

    const error = await firstValueFrom(http.get(API_URL)).catch((caught: unknown) => caught);

    expect(error).toBeInstanceOf(SessionEndedError);
    expect(store.login).toHaveBeenCalledWith(router.url);
    controller.expectNone(API_URL);
  });

  it('authInterceptor_OnOtherError_DoesNotRefresh', async () => {
    const result = firstValueFrom(http.get(API_URL)).catch((error: unknown) => error);

    const request = await vi.waitFor(() => controller.expectOne(API_URL));
    request.flush({}, { status: HttpStatusCode.Forbidden, statusText: 'Forbidden' });
    await result;

    expect(store.forceRefresh).not.toHaveBeenCalled();
    expect(store.login).not.toHaveBeenCalled();
  });
});
