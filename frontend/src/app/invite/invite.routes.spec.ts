import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';

import { APP_CONFIG } from '@shared/config/app-config';
import { authInterceptor } from '@shared/security/auth.interceptor';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { SessionEndedError } from '@shared/security/session-errors';

import { InvitePage } from '@invite/invite-page/invite-page';
import { INVITE_ROUTES } from '@invite/invite.routes';

const VERIFY_URL = '/api/core/v1/employee-invites/verify';

describe('invite routes', () => {
  const login = vi.fn();
  const isAuthenticated = vi.fn();
  const accessToken = vi.fn();

  function configure(): void {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'invite', children: INVITE_ROUTES }], withComponentInputBinding()),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideTranslateService(),
        {
          provide: APP_CONFIG,
          useValue: {
            apiBaseUrl: '',
            authUrl: 'https://auth.healthyfac.test',
            authRealm: 'r',
            authClientId: 'c',
            primeUiLicenseKey: 'k',
          },
        },
        { provide: AuthStore, useValue: { login, isAuthenticated, accessToken, email: () => 'me@healthyfac.test' } },
        { provide: MessageService, useValue: { add: vi.fn() } },
      ],
    });
  }

  beforeEach(() => {
    login.mockReset();
    isAuthenticated.mockReset().mockReturnValue(false);
    accessToken.mockReset().mockRejectedValue(new SessionEndedError('The session has ended.'));
  });

  afterEach(() => {
    window.history.replaceState(null, '', '/');
  });

  it('inviteRoute_WithTokenFragmentAndNoSession_VerifiesWithoutLoginAndClearsTheFragment', async () => {
    configure();
    const http = TestBed.inject(HttpTestingController);
    window.history.replaceState(null, '', '/invite#token=abc');
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/invite');
    TestBed.tick();

    const request = await vi.waitFor(() => http.expectOne(VERIFY_URL));
    expect(request.request.body).toEqual({ token: 'abc' });
    expect(request.request.urlWithParams).toBe(VERIFY_URL);
    expect(request.request.headers.has('Authorization')).toBe(false);
    expect(login).not.toHaveBeenCalled();
    expect(window.location.hash).toBe('');
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(InvitePage);
  });

  it('inviteRoute_WithTokenFragmentAndASession_SendsNoVerifyRequest', async () => {
    isAuthenticated.mockReturnValue(true);
    configure();
    const http = TestBed.inject(HttpTestingController);
    window.history.replaceState(null, '', '/invite#token=abc');
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/invite');
    TestBed.tick();

    http.expectNone(VERIFY_URL);
    expect(login).not.toHaveBeenCalled();
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(InvitePage);
  });
});
