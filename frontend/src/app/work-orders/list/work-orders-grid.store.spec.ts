import { ApplicationRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import type { ActionResult } from '@shared/actions/app-action';
import { SortDirection } from '@shared/grid/requests/grid-request';
import { LookupsApi } from '@shared/lookups/lookups-api';
import { AuthStore } from '@shared/security/auth.store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import {
  WorkOrderCategory,
  WorkOrderPriority,
  type WorkOrderRow,
  WorkOrderSortColumn,
  WorkOrderStatus,
} from '@work-orders/work-order';
import { WorkOrderActionType } from '@work-orders/work-order-actions';
import { WorkOrdersApi } from '@work-orders/work-orders-api';

import { WorkOrdersGridStore } from './work-orders-grid.store';

const EMPTY_PAGE = { items: [], totalCount: 0 };

const ROW: WorkOrderRow = {
  id: 'wo-1',
  title: 'Replace filter',
  status: WorkOrderStatus.Staged,
  priority: WorkOrderPriority.High,
  category: WorkOrderCategory.Repair,
  assigneeId: 'e1',
  assigneeName: 'Ann Lee',
  machineId: null,
  machineName: null,
  isMachineDeleted: false,
  plannedStartLocal: '2027-03-28T09:00:00',
  plannedStartUtc: '2027-03-28T06:00:00Z',
  estimateMinutes: 60,
  isDelayed: false,
  createdAt: '2027-03-01T08:00:00Z',
  rowVersion: 7,
};

describe('WorkOrdersGridStore', () => {
  const lastResult = signal<ActionResult | null>(null);
  const dispatch = vi.fn();
  const search = vi.fn();
  const employees = vi.fn();
  const machines = vi.fn();

  async function setup(
    options: { canManage?: boolean; isListHidden?: boolean } = {},
  ): Promise<InstanceType<typeof WorkOrdersGridStore>> {
    TestBed.configureTestingModule({
      providers: [
        WorkOrdersGridStore,
        { provide: WorkOrdersApi, useValue: { search } },
        { provide: LookupsApi, useValue: { employees, machines } },
        { provide: ActionStore, useValue: { dispatch, lastResult } },
        {
          provide: AuthStore,
          useValue: { hasPermission: (permission: string) => permission !== 'work-orders:manage' || options.canManage },
        },
        {
          provide: CLOCK,
          useValue: { now: () => Temporal.Instant.from('2027-01-01T00:00:00Z'), timeZoneId: () => 'Europe/Kyiv' },
        },
      ],
    });
    const store = TestBed.inject(WorkOrdersGridStore);
    if (options.isListHidden !== true) {
      store.setListShown(true);
    }
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    return store;
  }

  beforeEach(() => {
    lastResult.set(null);
    dispatch.mockReset();
    search.mockReset().mockReturnValue(of(EMPTY_PAGE));
    employees.mockReset().mockReturnValue(of([]));
    machines.mockReset().mockReturnValue(of([]));
  });

  async function reloadsAfter(type: string): Promise<number> {
    await setup();
    const callsBefore = search.mock.calls.length;

    lastResult.set({ type, sequence: 1 });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    return search.mock.calls.length - callsBefore;
  }

  it('workOrders_WhileTheListIsHidden_SendsNoSearchAndSendsOneWhenItShows', async () => {
    const store = await setup({ isListHidden: true });
    const hiddenCalls = search.mock.calls.length;

    store.setListShown(true);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(hiddenCalls).toBe(0);
    expect(search.mock.calls.length).toBe(1);
  });

  it('lastResult_WithSavedType_ReloadsTheRows', async () => {
    const reloads = await reloadsAfter(WorkOrderActionType.Saved);

    expect(reloads).toBe(1);
  });

  it('lastResult_WithConflictType_ReloadsTheRows', async () => {
    const reloads = await reloadsAfter(WorkOrderActionType.Conflict);

    expect(reloads).toBe(1);
  });

  it('lastResult_WithOtherType_KeepsTheRows', async () => {
    const reloads = await reloadsAfter('machines.saved');

    expect(reloads).toBe(0);
  });

  it('setFilter_WithTitle_SearchesWithTheFilterAndResetsTheOffset', async () => {
    const store = await setup();
    store.setPage(30, 10);

    store.setFilter({ title: 'pump' });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    const body = search.mock.lastCall?.[0];
    expect(body.offset).toBe(0);
    expect(body.filter.title).toEqual({ value: 'pump' });
  });

  it('setSort_WithPlannedStart_SearchesWithTheSort', async () => {
    const store = await setup();

    store.setSort({ column: WorkOrderSortColumn.PlannedStart, direction: SortDirection.Descending });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(search.mock.lastCall?.[0].sort).toEqual({ column: 'plannedStart', direction: 'desc' });
  });

  it('requestRowAction_WithRow_DispatchesTheActionWithTheRowVersion', async () => {
    const store = await setup();

    store.requestRowAction(WorkOrderActionType.Move, ROW);

    expect(dispatch).toHaveBeenCalledWith({
      type: WorkOrderActionType.Move,
      payload: { workOrderId: 'wo-1', rowVersion: 7 },
    });
  });

  it('employeeLookup_BeforeAnyRequest_SendsNoLookup', async () => {
    await setup({ canManage: true });

    expect(employees).not.toHaveBeenCalled();
    expect(machines).not.toHaveBeenCalled();
  });

  it('employeeLookup_WithAnImmediateRequest_SendsOneLookupWithTheText', async () => {
    const store = await setup({ canManage: true });

    store.employeeLookup.request({ text: 'an', isImmediate: true });
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(employees.mock.calls).toEqual([['an']]);
  });

  it('employeeLookup_WithASelectedAssigneeMissingFromTheNextPage_KeepsTheSelectedOption', async () => {
    const store = await setup({ canManage: true });
    employees.mockReturnValueOnce(of([{ id: 'e1', displayName: 'Ann Lee' }]));
    store.employeeLookup.request({ text: '', isImmediate: true });
    await new Promise((resolve) => setTimeout(resolve, 0));
    TestBed.tick();
    store.setFilter({ assigneeIds: ['e1'] });
    employees.mockReturnValueOnce(of([{ id: 'e2', displayName: 'Bob Ray' }]));

    store.employeeLookup.request({ text: 'bob', isImmediate: true });
    await new Promise((resolve) => setTimeout(resolve, 0));
    TestBed.tick();

    expect(store.employeeLookup.options().map((option) => option.id)).toEqual(['e1', 'e2']);
  });
});
