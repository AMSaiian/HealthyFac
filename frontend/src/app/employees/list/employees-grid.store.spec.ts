import { ApplicationRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import type { ActionResult } from '@shared/actions/app-action';
import { EmployeeRole } from '@shared/employee/employee';
import { SortDirection } from '@shared/grid/requests/grid-request';
import { AuthStore } from '@shared/security/auth.store';

import { EmployeeSortColumn, EmployeeStatus } from '@employees/employee';
import { EmployeeActionType } from '@employees/employee-actions';
import { EmployeesApi } from '@employees/employees-api';

import { EmployeesGridStore } from './employees-grid.store';
import { INITIAL_FILTER, isRetryable, toSearchBody } from './employees-grid.store.utils';

describe('toSearchBody', () => {
  it('toSearchBody_WithEmptyFilter_LeavesEveryFilterOut', () => {
    const body = toSearchBody({ offset: 0, limit: 10, sort: null, filter: INITIAL_FILTER });

    expect(JSON.parse(JSON.stringify(body.filter))).toEqual({});
    expect('sort' in body).toBe(false);
  });

  it('toSearchBody_WithEveryFilter_BuildsEachPart', () => {
    const body = toSearchBody({
      offset: 20,
      limit: 10,
      sort: { column: EmployeeSortColumn.Email, direction: SortDirection.Descending },
      filter: {
        name: 'ann',
        email: 'lee@',
        roles: [EmployeeRole.Supervisor],
        statuses: [EmployeeStatus.Active, EmployeeStatus.Provisioning],
      },
    });

    expect(body.filter).toEqual({
      name: { value: 'ann' },
      email: { value: 'lee@' },
      roles: { values: ['supervisor'] },
      statuses: { values: ['active', 'provisioning'] },
    });
    expect(body.sort).toEqual({ column: 'email', direction: 'desc' });
  });
});

describe('isRetryable', () => {
  it.each([
    [EmployeeStatus.Provisioning, true],
    [EmployeeStatus.ProvisioningFailed, true],
    [EmployeeStatus.Active, false],
    [EmployeeStatus.Deactivated, false],
  ])('isRetryable_ForStatus%s_Is%s', (status, expected) => {
    const result = isRetryable(status);

    expect(result).toBe(expected);
  });
});

describe('EmployeesGridStore', () => {
  const lastResult = signal<ActionResult | null>(null);
  const searchEmployees = vi.fn();
  const dispatch = vi.fn();
  const navigate = vi.fn();

  async function settle(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
  }

  async function setup(): Promise<InstanceType<typeof EmployeesGridStore>> {
    TestBed.configureTestingModule({
      providers: [
        EmployeesGridStore,
        { provide: EmployeesApi, useValue: { searchEmployees } },
        { provide: ActionStore, useValue: { dispatch, lastResult } },
        { provide: AuthStore, useValue: { hasPermission: () => true } },
        { provide: Router, useValue: { navigate } },
      ],
    });
    const store = TestBed.inject(EmployeesGridStore);
    await settle();
    return store;
  }

  beforeEach(() => {
    lastResult.set(null);
    searchEmployees.mockReset().mockReturnValue(of({ items: [], totalCount: 0 }));
    dispatch.mockReset();
    navigate.mockReset();
  });

  it('setFilter_WithRole_SearchesWithTheFilterAndResetsTheOffset', async () => {
    const store = await setup();
    store.setPage(30, 10);

    store.setFilter({ roles: [EmployeeRole.Worker] });
    await settle();

    const body = searchEmployees.mock.lastCall?.[0];
    expect(body.offset).toBe(0);
    expect(body.filter.roles).toEqual({ values: ['worker'] });
  });

  it('requestRetry_WithEmployee_DispatchesTheRetryAction', async () => {
    const store = await setup();

    store.requestRetry('e1');

    expect(dispatch).toHaveBeenCalledWith({
      type: EmployeeActionType.RetryProvisioning,
      payload: { employeeId: 'e1' },
    });
  });

  it('openShifts_AndOpenSchedule_NavigateToTheEmployeePages', async () => {
    const store = await setup();

    store.openShifts('e1');
    store.openSchedule('e1');

    expect(navigate).toHaveBeenNthCalledWith(1, ['/employees', 'e1', 'shifts']);
    expect(navigate).toHaveBeenNthCalledWith(2, ['/employees', 'e1', 'schedule']);
  });

  it('lastResult_WithSavedType_ReloadsTheEmployees', async () => {
    await setup();
    const before = searchEmployees.mock.calls.length;

    lastResult.set({ type: EmployeeActionType.Saved, sequence: 1 });
    await settle();

    expect(searchEmployees.mock.calls.length - before).toBe(1);
  });
});
