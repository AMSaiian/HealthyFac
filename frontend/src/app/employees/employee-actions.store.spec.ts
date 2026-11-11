import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import { EmployeeRole } from '@shared/employee/employee';
import { LanguageStore } from '@shared/i18n/language.store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import { EmployeeActionType, OPEN_INVITE_EXISTS_DETAIL } from '@employees/employee-actions';
import { EmployeeActionsStore } from '@employees/employee-actions.store';
import { EmployeesApi } from '@employees/employees-api';

describe('EmployeeActionsStore', () => {
  const createInvite = vi.fn();
  const resendInvite = vi.fn();
  const retryProvisioning = vi.fn();
  const complete = vi.fn();
  const cancel = vi.fn();
  const add = vi.fn();

  function setup(): InstanceType<typeof EmployeeActionsStore> {
    TestBed.configureTestingModule({
      providers: [
        EmployeeActionsStore,
        { provide: EmployeesApi, useValue: { createInvite, resendInvite, retryProvisioning } },
        { provide: ActionStore, useValue: { complete, cancel } },
        { provide: MessageService, useValue: { add } },
        { provide: LanguageStore, useValue: { language: () => 'en' } },
        {
          provide: CLOCK,
          useValue: { now: () => Temporal.Instant.from('2027-06-01T00:00:00Z'), timeZoneId: () => 'Europe/Kyiv' },
        },
        {
          provide: TranslateService,
          useValue: { instant: (key: string, params?: object) => `${key}${JSON.stringify(params ?? {})}` },
        },
      ],
    });
    return TestBed.inject(EmployeeActionsStore);
  }

  beforeEach(() => {
    createInvite.mockReset().mockReturnValue(of({ id: 'i1' }));
    resendInvite.mockReset().mockReturnValue(of({ expiresAt: '2027-06-20T10:00:00Z' }));
    retryProvisioning.mockReset().mockReturnValue(of(undefined));
    complete.mockReset();
    cancel.mockReset();
    add.mockReset();
  });

  it('submitInvite_WithEmailAndRole_SendsTheTrimmedEmailAndCompletesWithSaved', () => {
    const store = setup();
    store.start({ type: EmployeeActionType.Invite, payload: {} });

    store.submitInvite({
      email: ' new@healthyfac.test ',
      roleCode: EmployeeRole.Supervisor,
      firstName: '',
      lastName: '',
    });

    expect(createInvite).toHaveBeenCalledWith({ email: 'new@healthyfac.test', roleCode: 'supervisor' });
    expect(complete).toHaveBeenCalledWith(EmployeeActionType.Saved);
  });

  it('submitInvite_WithNames_SendsTheTrimmedNamesAndLeavesBlankOnesOut', () => {
    const store = setup();
    store.start({ type: EmployeeActionType.Invite, payload: {} });

    store.submitInvite({
      email: 'new@healthyfac.test',
      roleCode: EmployeeRole.Worker,
      firstName: ' Olena ',
      lastName: '   ',
    });

    expect(createInvite).toHaveBeenCalledWith({
      email: 'new@healthyfac.test',
      roleCode: 'worker',
      firstName: 'Olena',
    });
  });

  it('submitInvite_With409OpenInvite_ShowsTheMessageThatPointsToThePendingInvites', () => {
    createInvite.mockReturnValue(
      throwError(
        () => new HttpErrorResponse({ status: HttpStatusCode.Conflict, error: { detail: OPEN_INVITE_EXISTS_DETAIL } }),
      ),
    );
    const store = setup();
    store.start({ type: EmployeeActionType.Invite, payload: {} });

    store.submitInvite({ email: 'new@healthyfac.test', roleCode: EmployeeRole.Worker, firstName: '', lastName: '' });

    expect(add).toHaveBeenCalledWith(
      expect.objectContaining({ detail: expect.stringContaining('EMPLOYEES.TOASTS.OPEN_INVITE_EXISTS') }),
    );
    expect(complete).not.toHaveBeenCalled();
    expect(cancel).not.toHaveBeenCalled();
  });

  it('submitInvite_With409AlreadyEmployed_ShowsTheEmployedMessage', () => {
    createInvite.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict, error: { detail: 'other' } })),
    );
    const store = setup();
    store.start({ type: EmployeeActionType.Invite, payload: {} });

    store.submitInvite({ email: 'new@healthyfac.test', roleCode: EmployeeRole.Worker, firstName: '', lastName: '' });

    expect(add).toHaveBeenCalledWith(
      expect.objectContaining({ detail: expect.stringContaining('EMPLOYEES.TOASTS.ALREADY_EMPLOYEE') }),
    );
  });

  it('start_OnResend_ResendsAndShowsTheNewExpiryInTheCompanyZone', () => {
    const store = setup();

    store.start({ type: EmployeeActionType.Resend, payload: { inviteId: 'i1' } });

    expect(resendInvite).toHaveBeenCalledWith('i1');
    expect(add).toHaveBeenCalledWith(expect.objectContaining({ detail: expect.stringContaining('13:00') }));
    expect(complete).toHaveBeenCalledWith(EmployeeActionType.Saved);
  });

  it('start_OnRetryWith422_ShowsTheRuleToastAndCancelsTheAction', () => {
    retryProvisioning.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: HttpStatusCode.UnprocessableEntity })),
    );
    const store = setup();

    store.start({ type: EmployeeActionType.RetryProvisioning, payload: { employeeId: 'e1' } });

    expect(add).toHaveBeenCalledWith(
      expect.objectContaining({ detail: expect.stringContaining('EMPLOYEES.TOASTS.RULE') }),
    );
    expect(cancel).toHaveBeenCalledTimes(1);
    expect(complete).not.toHaveBeenCalled();
  });

  it('start_OnRetry_CallsTheEndpointWithTheEmployee', () => {
    const store = setup();

    store.start({ type: EmployeeActionType.RetryProvisioning, payload: { employeeId: 'e1' } });

    expect(retryProvisioning).toHaveBeenCalledWith('e1');
    expect(complete).toHaveBeenCalledWith(EmployeeActionType.Saved);
  });
});
