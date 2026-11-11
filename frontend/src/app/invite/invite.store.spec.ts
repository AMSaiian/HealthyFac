import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { EmployeeRole } from '@shared/employee/employee';
import { AuthStore } from '@shared/security/auth.store';

import { InviteApi, type InviteDetails } from '@invite/invite-api';
import { InviteLocation } from '@invite/invite-location';

import { InviteStore } from './invite.store';

const DETAILS: InviteDetails = {
  email: 'new@healthyfac.test',
  companyName: 'Test Company',
  roleCode: EmployeeRole.Worker,
  firstName: 'Ann',
  lastName: null,
  expiresAt: '2027-06-20T10:00:00Z',
};

describe('InviteStore', () => {
  const verify = vi.fn();
  const accept = vi.fn();
  const readToken = vi.fn();
  const clearFragment = vi.fn();
  const add = vi.fn();
  const logout = vi.fn();
  const isAuthenticated = vi.fn();

  function setup(): InstanceType<typeof InviteStore> {
    TestBed.configureTestingModule({
      providers: [
        InviteStore,
        { provide: InviteApi, useValue: { verify, accept } },
        { provide: InviteLocation, useValue: { readToken, clearFragment } },
        { provide: AuthStore, useValue: { isAuthenticated, logout, email: () => 'me@healthyfac.test' } },
        { provide: MessageService, useValue: { add } },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
      ],
    });
    return TestBed.inject(InviteStore);
  }

  beforeEach(() => {
    readToken.mockReset().mockReturnValue('tok-1');
    clearFragment.mockReset();
    verify.mockReset().mockReturnValue(of(DETAILS));
    accept.mockReset().mockReturnValue(of({ employeeId: 'e1' }));
    add.mockReset();
    logout.mockReset().mockResolvedValue(undefined);
    isAuthenticated.mockReset().mockReturnValue(false);
  });

  it('start_WithoutToken_SendsNoRequestAndShowsTheInvalidView', () => {
    readToken.mockReturnValue(null);

    const store = setup();

    expect(verify).not.toHaveBeenCalled();
    expect(store.view()).toBe('invalid');
  });

  it('start_WithToken_ClearsTheFragmentAndVerifiesTheInvite', () => {
    const store = setup();

    expect(clearFragment).toHaveBeenCalledTimes(1);
    expect(verify).toHaveBeenCalledWith('tok-1');
    expect(store.view()).toBe('form');
    expect(store.details()).toEqual(DETAILS);
  });

  it('start_WithVerify404_ShowsTheInvalidView', () => {
    verify.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.NotFound })));

    const store = setup();

    expect(store.view()).toBe('invalid');
    expect(add).not.toHaveBeenCalled();
  });

  it('start_WithVerify500_ShowsTheFailedViewAndAToast', () => {
    verify.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.InternalServerError })));

    const store = setup();

    expect(store.view()).toBe('failed');
    expect(add).toHaveBeenCalledTimes(1);
  });

  it('accept_WithNames_SendsTheTokenAndTheNamesAndShowsSuccess', () => {
    const store = setup();

    store.accept({ firstName: 'Ann', lastName: 'Lee' });

    expect(accept).toHaveBeenCalledWith('tok-1', { firstName: 'Ann', lastName: 'Lee' });
    expect(store.view()).toBe('success');
    expect(store.isSaving()).toBe(false);
  });

  it('accept_With409_ShowsTheEmployedView', () => {
    accept.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict })));
    const store = setup();

    store.accept({ firstName: 'Ann', lastName: 'Lee' });

    expect(store.view()).toBe('employed');
  });

  it('accept_With404_ShowsTheInvalidView', () => {
    accept.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.NotFound })));
    const store = setup();

    store.accept({ firstName: 'Ann', lastName: 'Lee' });

    expect(store.view()).toBe('invalid');
  });

  it('start_WithASession_ShowsTheSignedInViewAndSendsNoRequest', () => {
    isAuthenticated.mockReturnValue(true);

    const store = setup();

    expect(store.view()).toBe('signedIn');
    expect(verify).not.toHaveBeenCalled();
    expect(accept).not.toHaveBeenCalled();
    expect(logout).not.toHaveBeenCalled();
  });

  it('signOutAndContinue_WithASession_LogsOutToTheInvitePageWithTheEncodedTokenInTheFragment', async () => {
    isAuthenticated.mockReturnValue(true);
    readToken.mockReturnValue('a+b=');
    const store = setup();

    await store.signOutAndContinue();

    expect(logout).toHaveBeenCalledWith('/invite#token=a%2Bb%3D');
  });
});
