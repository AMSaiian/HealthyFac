import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ApplicationRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import type { ActionResult } from '@shared/actions/app-action';
import { EmployeeStore } from '@shared/employee/employee.store';
import { AuthStore } from '@shared/security/auth.store';
import { Permission } from '@shared/security/permissions';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import { type DaySchedule, WorkOrderPriority, WorkOrderStatus } from '@work-orders/work-order';
import { WorkOrderActionType } from '@work-orders/work-order-actions';
import { ScheduleApi } from '@schedule/schedule-api';

import { EmployeeDayStore } from './employee-day.store';

const DAY: DaySchedule = {
  date: '2027-06-01',
  timeZoneId: 'Europe/Kyiv',
  source: 'template',
  blocks: [
    { startLocal: '08:00', endLocal: '12:00', startUtc: '2027-06-01T05:00:00Z', endUtc: '2027-06-01T09:00:00Z' },
  ],
  workOrders: [
    {
      id: 'wo-1',
      title: 'Replace filter',
      status: WorkOrderStatus.Staged,
      priority: WorkOrderPriority.High,
      plannedStartLocal: '2027-06-01T09:00:00',
      plannedStartUtc: '2027-06-01T06:00:00Z',
      estimateMinutes: 60,
      occupancyEndUtc: '2027-06-01T07:00:00Z',
      isDelayed: false,
      rowVersion: 2,
    },
  ],
};

describe('EmployeeDayStore', () => {
  const lastResult = signal<ActionResult | null>(null);
  const employeeDay = vi.fn();
  const dispatch = vi.fn();
  let currentNow = Temporal.Instant.from('2027-06-01T07:00:00Z');

  async function settle(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
  }

  async function setup(permissions: readonly string[] = []): Promise<InstanceType<typeof EmployeeDayStore>> {
    TestBed.configureTestingModule({
      providers: [
        EmployeeDayStore,
        { provide: ScheduleApi, useValue: { employeeDay } },
        { provide: ActionStore, useValue: { dispatch, lastResult } },
        { provide: AuthStore, useValue: { hasPermission: (permission: string) => permissions.includes(permission) } },
        { provide: EmployeeStore, useValue: { id: () => 'e1', name: () => 'Ann Lee' } },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
        {
          provide: CLOCK,
          useValue: { now: () => currentNow, timeZoneId: () => 'Europe/Kyiv' },
        },
      ],
    });
    const store = TestBed.inject(EmployeeDayStore);
    store.setEmployee('e1');
    await settle();
    return store;
  }

  afterEach(() => vi.useRealTimers());

  beforeEach(() => {
    lastResult.set(null);
    currentNow = Temporal.Instant.from('2027-06-01T07:00:00Z');
    dispatch.mockReset();
    employeeDay.mockReset().mockReturnValue(of(DAY));
  });

  it('rows_AfterLoad_HoldOneRowWithTheOwnNameAndTemporalTimes', async () => {
    const store = await setup();

    const [row] = store.rows();

    expect(employeeDay).toHaveBeenCalledWith('e1', '2027-06-01');
    expect(row.label).toBe('Ann Lee');
    expect(row.bars[0].start.toString()).toBe('2027-06-01T06:00:00Z');
  });

  it('setDate_WithAnotherDate_ReloadsTheDay', async () => {
    const store = await setup();

    store.setDate(Temporal.PlainDate.from('2027-06-05'));
    await settle();

    expect(employeeDay).toHaveBeenLastCalledWith('e1', '2027-06-05');
  });

  it('nextDay_AndPreviousDay_MoveTheDateByOneDay', async () => {
    const store = await setup();

    store.nextDay();
    store.nextDay();
    store.previousDay();
    await settle();

    expect(store.date()?.toString()).toBe('2027-06-02');
    expect(employeeDay).toHaveBeenLastCalledWith('e1', '2027-06-02');
  });

  it('isNotFound_With404_IsTrue', async () => {
    employeeDay.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.NotFound })));

    const store = await setup();

    expect(store.isNotFound()).toBe(true);
    expect(store.rows()).toEqual([]);
  });

  it('isNotFound_With500_IsFalse', async () => {
    employeeDay.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: HttpStatusCode.InternalServerError })),
    );

    const store = await setup();

    expect(store.isNotFound()).toBe(false);
  });

  it('lastResult_WithSavedType_ReloadsTheDay', async () => {
    await setup();
    const before = employeeDay.mock.calls.length;

    lastResult.set({ type: WorkOrderActionType.Saved, sequence: 1 });
    await settle();

    expect(employeeDay.mock.calls.length - before).toBe(1);
  });

  it('openWorkOrder_WithId_OpensTheDrawerAction', async () => {
    const store = await setup();

    store.openWorkOrder('wo-1');

    expect(dispatch).toHaveBeenCalledWith({ type: WorkOrderActionType.Open, payload: { workOrderId: 'wo-1' } });
  });

  it('canGoBack_WithEmployeesRead_IsTrue', async () => {
    const store = await setup([Permission.EmployeesRead]);

    expect(store.canGoBack()).toBe(true);
  });

  it('canGoBack_WithoutEmployeesRead_IsFalse', async () => {
    const store = await setup();

    expect(store.canGoBack()).toBe(false);
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
    expect(employeeDay.mock.calls.filter((call) => call[1] === '2027-06-02').length).toBe(1);
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
    expect(employeeDay.mock.calls.filter((call) => call[1] === '2027-06-02').length).toBe(0);
  });
});
