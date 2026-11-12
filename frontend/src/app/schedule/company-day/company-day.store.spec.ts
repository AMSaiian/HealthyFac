import { ApplicationRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import type { ActionResult } from '@shared/actions/app-action';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import { WorkOrderActionType } from '@work-orders/work-order-actions';
import { type CompanyDaySchedule, ScheduleApi } from '@schedule/schedule-api';

import { CompanyDayStore } from './company-day.store';

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

describe('CompanyDayStore', () => {
  const lastResult = signal<ActionResult | null>(null);
  const companyDay = vi.fn();
  let currentNow = Temporal.Instant.from('2027-06-01T07:00:00Z');

  async function settle(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
  }

  async function setup(): Promise<InstanceType<typeof CompanyDayStore>> {
    TestBed.configureTestingModule({
      providers: [
        CompanyDayStore,
        { provide: ScheduleApi, useValue: { companyDay } },
        { provide: ActionStore, useValue: { dispatch: vi.fn(), lastResult } },
        {
          provide: CLOCK,
          useValue: { now: () => currentNow, timeZoneId: () => 'Europe/Kyiv' },
        },
      ],
    });
    const store = TestBed.inject(CompanyDayStore);
    await settle();
    return store;
  }

  afterEach(() => vi.useRealTimers());

  beforeEach(() => {
    lastResult.set(null);
    currentNow = Temporal.Instant.from('2027-06-01T07:00:00Z');
    companyDay.mockReset().mockReturnValue(of(COMPANY_DAY));
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
});
