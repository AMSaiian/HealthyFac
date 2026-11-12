import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ApplicationRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { forkJoin, type Observable, of, Subject, throwError } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import type { ActionResult, AppAction } from '@shared/actions/app-action';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import { WorkOrdersGridStore } from '@work-orders/list/work-orders-grid.store';
import { INITIAL_FILTER } from '@work-orders/list/work-orders-grid.store.utils';
import { WorkOrderCategory, WorkOrderPriority, type WorkOrderRow, WorkOrderStatus } from '@work-orders/work-order';
import { WorkOrderActionType } from '@work-orders/work-order-actions';
import { WorkOrdersApi } from '@work-orders/work-orders-api';

import { BoardStore } from './board.store';

function rowIn(status: WorkOrderStatus): WorkOrderRow {
  return {
    id: 'wo-1',
    title: 'Replace filter',
    status,
    priority: WorkOrderPriority.High,
    category: WorkOrderCategory.Repair,
    assigneeId: null,
    assigneeName: null,
    machineId: null,
    machineName: null,
    isMachineDeleted: false,
    plannedStartLocal: null,
    plannedStartUtc: null,
    estimateMinutes: 60,
    isDelayed: false,
    createdAt: '2027-05-01T08:00:00Z',
    rowVersion: 3,
  };
}

describe('BoardStore', () => {
  const lastResult = signal<ActionResult | null>(null);
  const active = signal<AppAction | null>(null);
  const filter = signal(INITIAL_FILTER);
  const setFilter = vi.fn();
  const dispatch = vi.fn();
  const search = vi.fn();
  const searchBatch = vi.fn((bodies: readonly unknown[]) =>
    forkJoin(bodies.map((body) => search(body) as Observable<unknown>)),
  );
  const changeStatus = vi.fn();
  const add = vi.fn();

  async function settle(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
  }

  async function setup(options: { canManage?: boolean } = {}): Promise<InstanceType<typeof BoardStore>> {
    TestBed.configureTestingModule({
      providers: [
        BoardStore,
        { provide: WorkOrdersGridStore, useValue: { filter, setFilter } },
        { provide: WorkOrdersApi, useValue: { searchBatch, changeStatus } },
        { provide: ActionStore, useValue: { dispatch, lastResult, active } },
        { provide: AuthStore, useValue: { hasPermission: () => options.canManage ?? true } },
        { provide: MessageService, useValue: { add } },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
        {
          provide: CLOCK,
          useValue: { now: () => Temporal.Instant.from('2027-06-01T09:00:00Z'), timeZoneId: () => 'Europe/Kyiv' },
        },
      ],
    });
    const store = TestBed.inject(BoardStore);
    await settle();
    return store;
  }

  const statusesOfCalls = (from: number): string[] =>
    search.mock.calls.slice(from).map((call) => call[0].filter.statuses.values[0]);

  beforeEach(() => {
    lastResult.set(null);
    active.set(null);
    setFilter.mockReset();
    filter.set(INITIAL_FILTER);
    dispatch.mockReset();
    search.mockReset().mockReturnValue(of({ items: [], totalCount: 0 }));
    searchBatch.mockClear();
    changeStatus.mockReset().mockReturnValue(of(undefined));
    add.mockReset();
  });

  it('reloadAll_OnFirstLoad_SearchesEveryColumnWithTodayForTheScheduledOnes', async () => {
    await setup();

    expect(new Set(statusesOfCalls(0))).toEqual(
      new Set(['backlog', 'staged', 'inProgress', 'blocked', 'completed', 'rejected']),
    );
    const staged = search.mock.calls.find((call) => call[0].filter.statuses.values[0] === 'staged')?.[0];
    const backlog = search.mock.calls.find((call) => call[0].filter.statuses.values[0] === 'backlog')?.[0];
    expect(staged.filter.plannedStart).toEqual({ from: '2027-05-31T21:00:00Z', to: '2027-06-01T21:00:00Z' });
    expect(backlog.filter.plannedStart).toBeUndefined();
  });

  it('filter_WhenChanged_ReloadsEveryColumnFromOffsetZero', async () => {
    await setup();
    const before = search.mock.calls.length;

    filter.set({ ...INITIAL_FILTER, title: 'pump' });
    await settle();

    expect(search.mock.calls.length - before).toBe(6);
    expect(search.mock.lastCall?.[0].offset).toBe(0);
    expect(search.mock.lastCall?.[0].filter.title).toEqual({ value: 'pump' });
  });

  it('drop_BetweenScheduledColumns_ChangesTheStatusWithTheRowVersionAndReloadsTwoColumns', async () => {
    const store = await setup();
    const before = search.mock.calls.length;

    store.drop(rowIn(WorkOrderStatus.Staged), WorkOrderStatus.Blocked);
    await settle();

    expect(changeStatus).toHaveBeenCalledWith('wo-1', { status: 'blocked', rowVersion: 3 }, expect.anything());
    expect(new Set(statusesOfCalls(before))).toEqual(new Set(['blocked', 'staged']));
  });

  it('drop_OnCompleted_ReloadsEveryColumn', async () => {
    const store = await setup();
    const before = search.mock.calls.length;

    store.drop(rowIn(WorkOrderStatus.InProgress), WorkOrderStatus.Completed);
    await settle();

    expect(search.mock.calls.length - before).toBe(6);
  });

  it('drop_With409_ShowsTheConflictToastAndReloadsTwoColumns', async () => {
    changeStatus.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict })));
    const store = await setup();
    const before = search.mock.calls.length;

    store.drop(rowIn(WorkOrderStatus.Staged), WorkOrderStatus.Blocked);
    await settle();

    expect(add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'WORK_ORDERS.TOASTS.CONFLICT' }));
    expect(new Set(statusesOfCalls(before))).toEqual(new Set(['blocked', 'staged']));
  });

  it('drop_With422_ShowsTheRuleToastAndReloadsNothing', async () => {
    changeStatus.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: HttpStatusCode.UnprocessableEntity })),
    );
    const store = await setup();
    const before = search.mock.calls.length;

    store.drop(rowIn(WorkOrderStatus.Staged), WorkOrderStatus.Blocked);
    await settle();

    expect(add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'WORK_ORDERS.TOASTS.RULE' }));
    expect(search.mock.calls.length).toBe(before);
    expect(store.columns().staged.isLoading).toBe(false);
  });

  it('drop_FromBacklogToStagedAsSupervisor_OpensTheAssignActionAndReloadsAfterItsResult', async () => {
    const store = await setup();
    const before = search.mock.calls.length;

    store.drop(rowIn(WorkOrderStatus.Backlog), WorkOrderStatus.Staged);
    lastResult.set({ type: WorkOrderActionType.Saved, sequence: 1 } satisfies ActionResult);
    await settle();

    expect(dispatch).toHaveBeenCalledWith(expect.objectContaining({ type: WorkOrderActionType.Assign }));
    expect(changeStatus).not.toHaveBeenCalled();
    expect(new Set(statusesOfCalls(before))).toEqual(new Set(['backlog', 'staged']));
  });

  it.each([
    [WorkOrderStatus.Backlog, WorkOrderStatus.Blocked, true],
    [WorkOrderStatus.Backlog, WorkOrderStatus.Staged, false],
    [WorkOrderStatus.Staged, WorkOrderStatus.Backlog, false],
  ])('drop_Refused_From%sTo%sWithManage%s_CallsNothing', async (source, target, canManage) => {
    const store = await setup({ canManage });

    store.drop(rowIn(source), target);
    await settle();

    expect(changeStatus).not.toHaveBeenCalled();
    expect(dispatch).not.toHaveBeenCalled();
  });

  it('drop_With409OnACompletion_LeavesNoColumnLoading', async () => {
    changeStatus.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict })));
    const store = await setup();

    store.drop(rowIn(WorkOrderStatus.InProgress), WorkOrderStatus.Completed);
    await settle();

    const loading = Object.values(store.columns()).filter((column) => column.isLoading);
    expect(loading).toEqual([]);
  });

  it('drop_WhileAnotherDropRuns_SendsBothAndLeavesNoColumnLoading', async () => {
    const first = new Subject<undefined>();
    const second = new Subject<undefined>();
    changeStatus.mockReturnValueOnce(first).mockReturnValueOnce(second);
    const store = await setup();

    store.drop(rowIn(WorkOrderStatus.Staged), WorkOrderStatus.Blocked);
    store.drop({ ...rowIn(WorkOrderStatus.Staged), id: 'wo-2' }, WorkOrderStatus.Blocked);
    first.next(undefined);
    first.complete();
    second.next(undefined);
    second.complete();
    await settle();

    expect(changeStatus).toHaveBeenCalledTimes(2);
    expect(Object.values(store.columns()).filter((column) => column.isLoading)).toEqual([]);
  });

  it('drop_AfterACancelledAssign_DoesNotKeepThePendingDrop', async () => {
    const store = await setup();
    const before = search.mock.calls.length;

    store.drop(rowIn(WorkOrderStatus.Backlog), WorkOrderStatus.Staged);
    active.set({ type: WorkOrderActionType.Assign, payload: {} });
    await settle();
    active.set(null);
    await settle();
    lastResult.set({ type: WorkOrderActionType.Saved, sequence: 1 });
    await settle();

    expect(store.pendingDrop()).toBeNull();
    expect(search.mock.calls.length - before).toBe(6);
  });

  it('lastResult_WithoutAPendingDrop_ReloadsEveryColumnWithoutEmptyingIt', async () => {
    search.mockReturnValue(of({ items: [rowIn(WorkOrderStatus.Staged)], totalCount: 40 }));
    const store = await setup();
    const before = search.mock.calls.length;

    lastResult.set({ type: WorkOrderActionType.Saved, sequence: 1 });
    TestBed.tick();

    expect(search.mock.calls.length - before).toBe(6);
    expect(store.columns().staged.window.totalCount).toBe(40);
  });

  it('lastResult_AfterACreateInBacklog_ReloadsOnlyTheBacklogColumn', async () => {
    await setup();
    const before = search.mock.calls.length;

    lastResult.set({ type: WorkOrderActionType.Saved, sequence: 1, detail: { status: WorkOrderStatus.Backlog } });
    TestBed.tick();

    expect(statusesOfCalls(before)).toEqual(['backlog']);
  });

  it('setColumnVisible_WithAColumnHidden_SendsNoSearchForItOnReload', async () => {
    const store = await setup();
    store.setColumnVisible(WorkOrderStatus.Blocked, false);
    const before = search.mock.calls.length;

    store.reloadAll();
    await settle();

    expect(statusesOfCalls(before)).toHaveLength(5);
    expect(statusesOfCalls(before)).not.toContain('blocked');
  });

  it('setColumnVisible_WithAHiddenColumnShown_LoadsIt', async () => {
    const store = await setup();
    store.setColumnVisible(WorkOrderStatus.Blocked, false);
    const before = search.mock.calls.length;

    store.setColumnVisible(WorkOrderStatus.Blocked, true);
    await settle();

    expect(statusesOfCalls(before)).toEqual(['blocked']);
  });

  it('setColumnVisible_WithTheLastVisibleColumn_KeepsIt', async () => {
    const store = await setup();
    for (const status of store.visibleStatuses().slice(1)) {
      store.setColumnVisible(status, false);
    }
    const last = store.visibleStatuses()[0];

    store.setColumnVisible(last, false);

    expect(store.visibleStatuses()).toEqual([last]);
  });

  it('reorder_WithANewOrder_MovesTheVisibleColumnsAndKeepsAHiddenOneInItsSlot', async () => {
    const store = await setup();
    store.setColumnVisible(WorkOrderStatus.Staged, false);

    store.reorder([
      WorkOrderStatus.Blocked,
      WorkOrderStatus.Backlog,
      WorkOrderStatus.InProgress,
      WorkOrderStatus.Completed,
      WorkOrderStatus.Rejected,
    ]);

    expect(store.layout().map((entry) => entry.column)).toEqual([
      WorkOrderStatus.Blocked,
      WorkOrderStatus.Staged,
      WorkOrderStatus.Backlog,
      WorkOrderStatus.InProgress,
      WorkOrderStatus.Completed,
      WorkOrderStatus.Rejected,
    ]);
    expect(store.visibleStatuses()).not.toContain(WorkOrderStatus.Staged);
  });

  it('setTitle_WithSeveralKeyPresses_WritesTheSharedFilterOnceAfterThePause', async () => {
    vi.useFakeTimers();
    try {
      const store = await setup();

      store.setTitle('p');
      store.setTitle('pu');
      store.setTitle('pump');
      expect(setFilter).not.toHaveBeenCalled();
      vi.advanceTimersByTime(300);

      expect(setFilter).toHaveBeenCalledTimes(1);
      expect(setFilter).toHaveBeenCalledWith({ title: 'pump' });
      expect(store.titleDraft()).toBe('pump');
    } finally {
      vi.useRealTimers();
    }
  });

  it('loadRange_WithTheLoadedWindowAgain_SendsNoSecondRequest', async () => {
    search.mockReturnValue(of({ items: [], totalCount: 500 }));
    const store = await setup();
    store.loadRange(WorkOrderStatus.Staged, { first: 20, last: 40 });
    await settle();
    const before = search.mock.calls.length;

    store.loadRange(WorkOrderStatus.Staged, { first: 20, last: 40 });
    await settle();

    expect(search.mock.calls.length - before).toBe(0);
  });

  const batchSizes = (from: number): number[] => searchBatch.mock.calls.slice(from).map((call) => call[0].length);

  it('reloadAll_OnFirstLoad_SendsOneBatchWithOneSearchForEachColumn', async () => {
    await setup();

    expect(batchSizes(0)).toEqual([6]);
  });

  it('drop_BetweenScheduledColumns_SendsOneBatchWithTheTwoTouchedColumns', async () => {
    const store = await setup();
    const before = searchBatch.mock.calls.length;

    store.drop(rowIn(WorkOrderStatus.Staged), WorkOrderStatus.InProgress);
    await settle();

    expect(batchSizes(before)).toEqual([2]);
  });

  it('drop_OnCompleted_SendsOneBatchWithEveryVisibleColumn', async () => {
    const store = await setup();
    const before = searchBatch.mock.calls.length;

    store.drop(rowIn(WorkOrderStatus.InProgress), WorkOrderStatus.Completed);
    await settle();

    expect(batchSizes(before)).toEqual([6]);
  });

  it('loadRange_WithAScroll_SendsOneBatchWithOneSearch', async () => {
    search.mockReturnValue(of({ items: [], totalCount: 500 }));
    const store = await setup();
    const before = searchBatch.mock.calls.length;

    store.loadRange(WorkOrderStatus.Staged, { first: 20, last: 40 });
    await settle();

    expect(batchSizes(before)).toEqual([1]);
  });

  it('loadRange_WhileAnOlderRequestOfTheColumnRuns_DropsTheOlderPage', async () => {
    search.mockReturnValue(of({ items: [], totalCount: 500 }));
    const store = await setup();
    const older = new Subject<{ items: WorkOrderRow[]; totalCount: number }[]>();
    searchBatch.mockReturnValueOnce(older as never);
    store.loadRange(WorkOrderStatus.Staged, { first: 20, last: 40 });
    searchBatch.mockReturnValueOnce(
      of([{ items: [{ ...rowIn(WorkOrderStatus.Staged), id: 'newer' }], totalCount: 50 }]) as never,
    );
    store.loadRange(WorkOrderStatus.Staged, { first: 40, last: 60 });
    await settle();

    older.next([{ items: [{ ...rowIn(WorkOrderStatus.Staged), id: 'older' }], totalCount: 99 }]);
    older.complete();
    await settle();

    const window = store.columns()[WorkOrderStatus.Staged].window;
    expect(window.items.map((row) => row.id)).toEqual(['newer']);
    expect(window.totalCount).toBe(50);
  });

  it('loadRange_WithARangeInsideTheLoadedRows_SendsNoRequest', async () => {
    const rows = [
      rowIn(WorkOrderStatus.InProgress),
      rowIn(WorkOrderStatus.InProgress),
      rowIn(WorkOrderStatus.InProgress),
    ];
    search.mockImplementation((body) =>
      of(
        body.filter.statuses.values[0] === 'inProgress'
          ? { items: rows, totalCount: rows.length }
          : { items: [], totalCount: 0 },
      ),
    );
    const store = await setup();
    const before = search.mock.calls.length;

    store.loadRange(WorkOrderStatus.InProgress, { first: 0, last: rows.length });
    await settle();

    expect(search.mock.calls.length - before).toBe(0);
  });

  it('drop_BetweenScheduledColumns_SendsOneSearchForEachTouchedColumn', async () => {
    const store = await setup();
    const before = search.mock.calls.length;

    store.drop(rowIn(WorkOrderStatus.Staged), WorkOrderStatus.InProgress);
    await settle();

    expect(search.mock.calls.length - before).toBe(2);
  });

  it('loadRange_WithAnotherWindow_SendsOneRequestWithThatWindow', async () => {
    search.mockReturnValue(of({ items: [], totalCount: 500 }));
    const store = await setup();
    const before = search.mock.calls.length;

    store.loadRange(WorkOrderStatus.Staged, { first: 20, last: 40 });
    await settle();

    expect(search.mock.calls.length - before).toBe(1);
    expect(search.mock.lastCall?.[0]).toEqual(expect.objectContaining({ offset: 20, limit: 30 }));
  });

  it('loadRange_WithAWindowOutsideTheLimits_SendsNoRequest', async () => {
    const store = await setup();
    const before = search.mock.calls.length;

    store.loadRange(WorkOrderStatus.Staged, { first: 0, last: -10 });
    store.loadRange(WorkOrderStatus.Staged, { first: -5, last: 10 });
    store.loadRange(WorkOrderStatus.Staged, { first: Number.NaN, last: 10 });
    await settle();

    expect(search.mock.calls.length - before).toBe(0);
  });

  it('loadRange_WithAVeryTallRange_CapsTheLimitAt100', async () => {
    search.mockReturnValue(of({ items: [], totalCount: 500 }));
    const store = await setup();

    store.loadRange(WorkOrderStatus.Staged, { first: 0, last: 500 });
    await settle();

    expect(search.mock.lastCall?.[0].limit).toBe(100);
  });
});
