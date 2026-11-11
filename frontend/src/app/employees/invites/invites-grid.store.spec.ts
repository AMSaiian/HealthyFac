import { ApplicationRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService } from 'primeng/api';
import { of } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import type { ActionResult } from '@shared/actions/app-action';

import { EmployeeActionType } from '@employees/employee-actions';
import type { InviteRow } from '@employees/employee';
import { EmployeesApi } from '@employees/employees-api';

import { InvitesGridStore } from './invites-grid.store';
import { INITIAL_FILTER, toSearchBody } from './invites-grid.store.utils';

describe('toSearchBody (invites)', () => {
  it('toSearchBody_WithEmptyEmail_LeavesTheFilterOut', () => {
    const body = toSearchBody({ offset: 0, limit: 10, sort: null, filter: INITIAL_FILTER });

    expect(body.filter.email).toBeUndefined();
  });

  it('toSearchBody_WithEmail_BuildsTheTextFilter', () => {
    const body = toSearchBody({ offset: 0, limit: 10, sort: null, filter: { email: 'new@' } });

    expect(body.filter.email).toEqual({ value: 'new@' });
  });
});

describe('InvitesGridStore', () => {
  const lastResult = signal<ActionResult | null>(null);
  const searchInvites = vi.fn();
  const dispatch = vi.fn();

  async function settle(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
  }

  async function setup(isActive = true, isConfirmed = true): Promise<InstanceType<typeof InvitesGridStore>> {
    TestBed.configureTestingModule({
      providers: [
        InvitesGridStore,
        { provide: EmployeesApi, useValue: { searchInvites } },
        { provide: ActionStore, useValue: { dispatch, lastResult } },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
        {
          provide: ConfirmationService,
          useValue: {
            confirm: (confirmation: { accept?: () => void; reject?: () => void }) =>
              isConfirmed ? confirmation.accept?.() : confirmation.reject?.(),
          },
        },
      ],
    });
    const store = TestBed.inject(InvitesGridStore);
    if (isActive) {
      store.activate();
    }
    await settle();
    return store;
  }

  beforeEach(() => {
    lastResult.set(null);
    searchInvites.mockReset().mockReturnValue(of({ items: [], totalCount: 0 }));
    dispatch.mockReset();
  });

  it('invites_BeforeActivation_SendNoRequest', async () => {
    await setup(false);

    expect(searchInvites).not.toHaveBeenCalled();
  });

  it('activate_AfterSetup_SendsOneRequest', async () => {
    const store = await setup(false);

    store.activate();
    await settle();

    expect(searchInvites).toHaveBeenCalledTimes(1);
  });

  it('requestResend_WithInvite_DispatchesTheResendAction', async () => {
    const store = await setup();

    store.requestResend('i1');

    expect(dispatch).toHaveBeenCalledWith({ type: EmployeeActionType.Resend, payload: { inviteId: 'i1' } });
  });

  it('lastResult_WithSavedType_ReloadsTheInvites', async () => {
    await setup();
    const before = searchInvites.mock.calls.length;

    lastResult.set({ type: EmployeeActionType.Saved, sequence: 1 });
    await settle();

    expect(searchInvites.mock.calls.length - before).toBe(1);
  });

  const row = { id: 'i1', email: 'a@b.test', rowVersion: 7 } as InviteRow;

  it('requestRevoke_WhenConfirmed_DispatchesOneRevokeWithTheRowVersion', async () => {
    const store = await setup();

    store.requestRevoke(row);

    expect(dispatch).toHaveBeenCalledTimes(1);
    expect(dispatch).toHaveBeenCalledWith({
      type: EmployeeActionType.Revoke,
      payload: { inviteId: 'i1', rowVersion: 7 },
    });
  });

  it('requestRevoke_WhenCancelled_DispatchesNothing', async () => {
    const store = await setup(true, false);

    store.requestRevoke(row);

    expect(dispatch).not.toHaveBeenCalled();
  });
});
