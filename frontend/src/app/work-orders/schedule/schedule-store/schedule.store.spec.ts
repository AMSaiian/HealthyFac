import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ApplicationRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';

import { ActionStore } from '@shared/actions/action.store';
import { EmployeeStore } from '@shared/employee/employee.store';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import type { ActionResult } from '@shared/actions/app-action';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import { WorkOrderActionType } from '@work-orders/work-order-actions';
import { WorkOrdersGridStore } from '@work-orders/list/work-orders-grid.store';
import { WorkOrdersApi } from '@work-orders/work-orders-api';
import { type CompanyDaySchedule, ScheduleApi } from '@schedule/schedule-api';

import { ScheduleStore } from './schedule.store';
import { RealtimeTopic } from '@shared/realtime/realtime';
import {
  createFakeRealtimeConnection,
  provideFakeRealtimeConnection,
} from '@shared/realtime/testing/fake-realtime-connection';

const EMPTY_FILTER = {
  title: '',
  statuses: [],
  priorities: [],
  categories: [],
  assigneeIds: [],
  machineIds: [],
  plannedStart: { start: null, end: null },
};

const COMPANY_DAY: CompanyDaySchedule = {
  date: '2027-06-01',
  timeZoneId: 'Europe/Kyiv',
  employees: [
    {
      employeeId: 'e1',
      firstName: 'Ann',
      lastName: 'Lee',
      day: { date: '2027-06-01', timeZoneId: 'Europe/Kyiv', source: 'template', blocks: [], workOrders: [] },
    },
    {
      employeeId: 'e2',
      firstName: 'Bo',
      lastName: 'Kim',
      day: { date: '2027-06-01', timeZoneId: 'Europe/Kyiv', source: 'template', blocks: [], workOrders: [] },
    },
  ],
};

describe('ScheduleStore', () => {
  const realtime = createFakeRealtimeConnection();
  const lastResult = signal<ActionResult | null>(null);
  const companyDay = vi.fn();
  const employeeDay = vi.fn();
  const search = vi.fn();
  const applySchedule = vi.fn();
  const confirm = vi.fn();
  const canManage = signal(true);
  const employeeResource = {
    hasValue: () => true,
    value: () => ({ id: 'e1', firstName: 'Ann', lastName: 'Lee', roleCode: 'worker' }),
  };
  let currentNow = Temporal.Instant.from('2027-06-01T07:00:00Z');

  async function settle(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
  }

  async function setup(): Promise<InstanceType<typeof ScheduleStore>> {
    TestBed.configureTestingModule({
      providers: [
        provideFakeRealtimeConnection(realtime),
        provideTranslateService(),
        ScheduleStore,
        { provide: ScheduleApi, useValue: { companyDay, employeeDay } },
        { provide: WorkOrdersApi, useValue: { search, applySchedule } },
        { provide: WorkOrdersGridStore, useValue: { filter: signal(EMPTY_FILTER) } },
        { provide: AuthStore, useValue: { hasPermission: () => canManage() } },
        { provide: EmployeeStore, useValue: { employee: employeeResource } },
        { provide: MessageService, useValue: { add: vi.fn() } },
        { provide: ConfirmationService, useValue: { confirm } },
        { provide: ActionStore, useValue: { dispatch: vi.fn(), lastResult } },
        {
          provide: CLOCK,
          useValue: { now: () => currentNow, timeZoneId: () => 'Europe/Kyiv' },
        },
      ],
    });
    const store = TestBed.inject(ScheduleStore);
    await settle();
    return store;
  }

  afterEach(() => vi.useRealTimers());

  const staged = (id: string): never =>
    ({
      id,
      title: id,
      status: 'staged',
      priority: 'medium',
      plannedStartLocal: '2027-06-01T09:00:00',
      plannedStartUtc: '2027-06-01T06:00:00Z',
      estimateMinutes: 60,
      occupancyEndUtc: '2027-06-01T07:00:00Z',
      isDelayed: false,
      rowVersion: 7,
    }) as never;

  const shiftBlock = {
    startUtc: '2027-06-01T05:00:00Z',
    endUtc: '2027-06-01T09:00:00Z',
    startLocal: '2027-06-01T08:00:00',
    endLocal: '2027-06-01T12:00:00',
  };
  const dayWithOrder: CompanyDaySchedule = {
    ...COMPANY_DAY,
    employees: [
      {
        ...COMPANY_DAY.employees[0],
        day: { ...COMPANY_DAY.employees[0].day, blocks: [shiftBlock], workOrders: [staged('w1')] },
      },
      { ...COMPANY_DAY.employees[1], day: { ...COMPANY_DAY.employees[1].day, blocks: [shiftBlock] } },
    ],
  };
  const backlogRow = { id: 'w9', title: 'w9', status: 'backlog', estimateMinutes: 30, rowVersion: 2 };
  const when = (hours: number): Temporal.Instant => Temporal.Instant.from(`2027-06-01T0${hours}:00:00Z`);

  beforeEach(() => {
    lastResult.set(null);
    currentNow = Temporal.Instant.from('2027-06-01T07:00:00Z');
    companyDay.mockReset().mockReturnValue(of(COMPANY_DAY));
    employeeDay.mockReset().mockReturnValue(of(COMPANY_DAY.employees[0].day));
    search.mockReset().mockReturnValue(of({ items: [], totalCount: 0 }));
    applySchedule.mockReset();
    confirm.mockReset();
    canManage.set(true);
  });

  it('hint_OfTheScheduleTopic_ReloadsTheDayOnceAfterOneSecond', async () => {
    await setup();
    vi.useFakeTimers();
    const callsBefore = companyDay.mock.calls.length;

    realtime.hints$.next({ topic: RealtimeTopic.Schedule });
    await vi.advanceTimersByTimeAsync(999);
    expect(companyDay.mock.calls.length).toBe(callsBefore);
    await vi.advanceTimersByTimeAsync(1);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    vi.useRealTimers();

    expect(companyDay.mock.calls.length - callsBefore).toBe(1);
  });

  it('rows_AfterLoad_HaveOneRowPerEmployeeWithTheFullName', async () => {
    const store = await setup();

    const labels = store.rows().map((row) => row.label);

    expect(companyDay).toHaveBeenCalledWith('2027-06-01');
    expect(labels).toEqual(['Ann Lee', 'Bo Kim']);
  });

  it('hasFailed_WhenTheRequestFails_IsTrueAndTheRowsStayEmpty', async () => {
    companyDay.mockReturnValue(throwError(() => new Error('gateway')));

    const store = await setup();

    expect(store.hasFailed()).toBe(true);
    expect(store.rows()).toEqual([]);
  });

  it('hasFailed_AfterASuccessfulLoad_IsFalse', async () => {
    const store = await setup();

    expect(store.hasFailed()).toBe(false);
  });

  it('setDate_WithAnotherDate_ReloadsTheDay', async () => {
    const store = await setup();

    store.setDate(Temporal.PlainDate.from('2027-06-09'));
    await settle();

    expect(companyDay).toHaveBeenLastCalledWith('2027-06-09');
  });

  it('lastResult_WithConflictType_ReloadsTheDay', async () => {
    await setup();
    const before = companyDay.mock.calls.length;

    lastResult.set({ type: WorkOrderActionType.Conflict, sequence: 1 });
    await settle();

    expect(companyDay.mock.calls.length - before).toBe(1);
  });

  it('lastResult_WithOtherType_KeepsTheDay', async () => {
    await setup();
    const before = companyDay.mock.calls.length;

    lastResult.set({ type: 'machines.saved', sequence: 1 });
    await settle();

    expect(companyDay.mock.calls.length - before).toBe(0);
  });

  it('now_FirstTick_ComesAtTheNextFullMinute', async () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'setInterval', 'clearInterval', 'clearTimeout'] });
    currentNow = Temporal.Instant.from('2027-06-01T07:00:20Z');
    const store = await setup();
    currentNow = Temporal.Instant.from('2027-06-01T07:01:00Z');

    await vi.advanceTimersByTimeAsync(39_999);
    const beforeTick = store.now().toString();
    await vi.advanceTimersByTimeAsync(1);

    expect(beforeTick).toBe('2027-06-01T07:00:20Z');
    expect(store.now().toString()).toBe('2027-06-01T07:01:00Z');
  });

  it('date_WhenMidnightPassesWhileTodayIsShown_MovesToTheNewDayAndLoadsItOnce', async () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'setInterval', 'clearInterval', 'clearTimeout'] });
    currentNow = Temporal.Instant.from('2027-06-01T20:59:30Z');
    const store = await setup();
    currentNow = Temporal.Instant.from('2027-06-01T21:00:00Z');

    await vi.advanceTimersByTimeAsync(30_000);
    await settle();

    expect(store.date()?.toString()).toBe('2027-06-02');
    expect(companyDay.mock.calls.filter((call) => call[0] === '2027-06-02').length).toBe(1);
  });

  it('date_WhenMidnightPassesWhileAnotherDateIsShown_StaysAndSendsNoRequest', async () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'setInterval', 'clearInterval', 'clearTimeout'] });
    currentNow = Temporal.Instant.from('2027-06-01T20:59:30Z');
    const store = await setup();
    store.setDate(Temporal.PlainDate.from('2027-06-05'));
    await settle();
    currentNow = Temporal.Instant.from('2027-06-01T21:00:00Z');

    await vi.advanceTimersByTimeAsync(30_000);
    await settle();

    expect(store.date()?.toString()).toBe('2027-06-05');
    expect(companyDay.mock.calls.filter((call) => call[0] === '2027-06-02').length).toBe(0);
  });

  it('rows_AsAWorker_ComeFromTheOwnDayAndSendNoCompanyRequestAndNoBacklogSearch', async () => {
    canManage.set(false);

    const store = await setup();

    expect(employeeDay).toHaveBeenCalledWith('e1', '2027-06-01');
    expect(companyDay).not.toHaveBeenCalled();
    expect(search).not.toHaveBeenCalled();
    expect(store.rows().map((item) => item.label)).toEqual(['Ann Lee']);
  });

  it('staging_AsASupervisor_SearchesTheBacklogOnce', async () => {
    await setup();

    expect(search).toHaveBeenCalledTimes(1);
    expect(JSON.stringify(search.mock.calls[0][0].filter.statuses)).toContain('backlog');
    expect(search.mock.calls[0][0].limit).toBe(100);
  });

  it('dropBar_OnAnotherRow_AddsOnePendingEntryAndRedrawsTheBar', async () => {
    companyDay.mockReturnValue(of(dayWithOrder));
    const store = await setup();

    store.dropBar({ workOrderId: 'w1', assigneeId: 'e2', startInstant: when(7) });

    expect(store.pending()).toEqual([
      {
        workOrderId: 'w1',
        rowVersion: 7,
        assigneeId: 'e2',
        plannedStartLocal: '2027-06-01T10:00:00',
        estimateMinutes: 60,
      },
    ]);
    expect(store.displayRows().map((item) => item.bars.length)).toEqual([0, 1]);
  });

  it('dropBar_BackToTheServerSlot_RemovesThePendingEntry', async () => {
    companyDay.mockReturnValue(of(dayWithOrder));
    const store = await setup();
    store.dropBar({ workOrderId: 'w1', assigneeId: 'e2', startInstant: when(7) });

    store.dropBar({ workOrderId: 'w1', assigneeId: 'e1', startInstant: when(6) });

    expect(store.pending()).toEqual([]);
  });

  it('pending_AfterADaySwitch_KeepsTheEntriesAndTheConflictsOfTheVisitedDay', async () => {
    companyDay.mockReturnValue(of(dayWithOrder));
    const store = await setup();
    store.dropBar({ workOrderId: 'w1', assigneeId: 'e2', startInstant: Temporal.Instant.from('2027-06-01T10:00:00Z') });
    await settle();

    store.nextDay();
    await settle();

    expect(store.pending().length).toBe(1);
    expect(store.conflictIds().has('w1')).toBe(true);
    expect(store.canApply()).toBe(false);
  });

  it('resizeBar_ToALongerEstimate_ChangesTheEstimateOfTheEntry', async () => {
    companyDay.mockReturnValue(of(dayWithOrder));
    const store = await setup();

    store.resizeBar({ workOrderId: 'w1', estimateMinutes: 120 });

    expect(store.pending().map((entry) => entry.estimateMinutes)).toEqual([120]);
    expect(store.canApply()).toBe(true);
  });

  it('unassignBar_OfAScheduledWorkOrder_ReturnsItToTheStagingList', async () => {
    companyDay.mockReturnValue(of(dayWithOrder));
    const store = await setup();

    store.unassignBar('w1');

    expect(store.pending().map((entry) => [entry.assigneeId, entry.plannedStartLocal])).toEqual([[null, null]]);
    expect(store.stagingItems().map((item) => item.id)).toEqual(['w1']);
  });

  it('dropBar_OfABacklogWorkOrderFromStaging_AssignsItAndLeavesTheStagingList', async () => {
    companyDay.mockReturnValue(of(dayWithOrder));
    search.mockReturnValue(of({ items: [backlogRow], totalCount: 1 }));
    const store = await setup();

    store.dropBar({ workOrderId: 'w9', assigneeId: 'e2', startInstant: when(8) });

    expect(store.pending().map((entry) => [entry.workOrderId, entry.assigneeId, entry.rowVersion])).toEqual([
      ['w9', 'e2', 2],
    ]);
    expect(store.stagingItems()).toEqual([]);
  });

  it('apply_WithPendingEntries_SendsThemAndClearsAndReloadsOnSuccess', async () => {
    companyDay.mockReturnValue(of(dayWithOrder));
    applySchedule.mockReturnValue(of({ workOrders: [] }));
    const store = await setup();
    store.resizeBar({ workOrderId: 'w1', estimateMinutes: 120 });
    const loads = companyDay.mock.calls.length;

    store.apply();
    await settle();

    expect(applySchedule.mock.calls[0][0].changes.map((entry: { workOrderId: string }) => entry.workOrderId)).toEqual([
      'w1',
    ]);
    expect(store.pending()).toEqual([]);
    expect(companyDay.mock.calls.length).toBeGreaterThan(loads);
  });

  it('apply_With422_SetsTheFailuresAndKeepsTheEntries', async () => {
    companyDay.mockReturnValue(of(dayWithOrder));
    applySchedule.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: HttpStatusCode.UnprocessableEntity,
            error: { changes: [{ workOrderId: 'w1', reason: 'slotTaken' }] },
          }),
      ),
    );
    const store = await setup();
    store.resizeBar({ workOrderId: 'w1', estimateMinutes: 120 });

    store.apply();
    await settle();

    expect(store.failures()).toEqual({ w1: 'WORK_ORDERS.SCHEDULE_TAB.REJECTIONS.SLOT_TAKEN' });
    expect(store.pending().length).toBe(1);
  });

  it('apply_With409_KeepsTheEntriesAndSetsNoFailures', async () => {
    companyDay.mockReturnValue(of(dayWithOrder));
    applySchedule.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict })));
    const store = await setup();
    store.resizeBar({ workOrderId: 'w1', estimateMinutes: 120 });

    store.apply();
    await settle();

    expect(store.failures()).toEqual({});
    expect(store.pending().length).toBe(1);
    expect(store.isApplying()).toBe(false);
  });

  it('discard_AfterTheConfirmation_ClearsTheEntriesAndReloads', async () => {
    companyDay.mockReturnValue(of(dayWithOrder));
    confirm.mockImplementation((request: { accept: () => void }) => request.accept());
    const store = await setup();
    store.resizeBar({ workOrderId: 'w1', estimateMinutes: 120 });
    const loads = companyDay.mock.calls.length;

    store.discard();
    await settle();

    expect(store.pending()).toEqual([]);
    expect(companyDay.mock.calls.length).toBeGreaterThan(loads);
  });
});
