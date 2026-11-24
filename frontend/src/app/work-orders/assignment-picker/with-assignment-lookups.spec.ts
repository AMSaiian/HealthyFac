import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { signalStore, withProps } from '@ngrx/signals';
import { of, Subject } from 'rxjs';

import { LookupsApi } from '@shared/lookups/lookups-api';
import { Temporal } from '@shared/time/temporal';

import type { FreeSpans, ScheduleDays } from '@work-orders/work-order';
import { WorkOrdersApi } from '@work-orders/work-orders-api';

import { withAssignmentLookups } from './with-assignment-lookups';

const Store = signalStore(
  withProps(() => ({ workOrderId: signal<string | undefined>('wo-1') })),
  withAssignmentLookups(),
);

function days(...dates: string[]): ScheduleDays {
  return {
    timeZoneId: 'Europe/Kyiv',
    days: dates.map((date) => ({ date, hasBlocks: true, longestFreeMinutes: 60 })),
  };
}

function range(from: string, to: string): { from: Temporal.PlainDate; to: Temporal.PlainDate } {
  return { from: Temporal.PlainDate.from(from), to: Temporal.PlainDate.from(to) };
}

describe('withAssignmentLookups', () => {
  const scheduleDays = vi.fn();
  const freeSpans = vi.fn();

  function setup(): InstanceType<typeof Store> {
    TestBed.configureTestingModule({
      providers: [
        Store,
        { provide: WorkOrdersApi, useValue: { scheduleDays, freeSpans } },
        { provide: LookupsApi, useValue: { employees: () => of([]) } },
      ],
    });
    return TestBed.inject(Store);
  }

  beforeEach(() => {
    scheduleDays.mockReset();
    freeSpans.mockReset();
  });

  it('loadDays_ForTheSameAssigneeTwice_AddsTheDaysAndPassesTheEditedWorkOrder', () => {
    scheduleDays.mockReturnValueOnce(of(days('2027-06-01'))).mockReturnValueOnce(of(days('2027-07-01')));
    const store = setup();

    store.loadDays({ assigneeId: 'e1', ...range('2027-05-31', '2027-07-11') });
    store.loadDays({ assigneeId: 'e1', ...range('2027-06-28', '2027-08-08') });

    expect(store.days().map((day) => day.date)).toEqual(['2027-06-01', '2027-07-01']);
    expect(scheduleDays).toHaveBeenLastCalledWith('e1', '2027-06-28', '2027-08-08', 'wo-1');
  });

  it('loadDays_ForAnotherAssignee_StartsOver', () => {
    scheduleDays.mockReturnValueOnce(of(days('2027-06-01'))).mockReturnValueOnce(of(days('2027-06-02')));
    const store = setup();

    store.loadDays({ assigneeId: 'e1', ...range('2027-05-31', '2027-07-11') });
    store.loadDays({ assigneeId: 'e2', ...range('2027-05-31', '2027-07-11') });

    expect(store.days().map((day) => day.date)).toEqual(['2027-06-02']);
  });

  it('loadSpans_WithTwoRequests_KeepsTheAnswerOfTheLatest', () => {
    const first = new Subject<FreeSpans>();
    const second = new Subject<FreeSpans>();
    freeSpans.mockReturnValueOnce(first).mockReturnValueOnce(second);
    const store = setup();

    store.loadSpans({ assigneeId: 'e1', date: Temporal.PlainDate.from('2027-06-01') });
    store.loadSpans({ assigneeId: 'e1', date: Temporal.PlainDate.from('2027-06-02') });
    first.next({ date: '2027-06-01', timeZoneId: 'Europe/Kyiv', spans: [] });
    second.next({
      date: '2027-06-02',
      timeZoneId: 'Europe/Kyiv',
      spans: [
        {
          startLocal: '2027-06-02T09:00:00',
          endLocal: '2027-06-02T10:00:00',
          startUtc: '2027-06-02T06:00:00Z',
          endUtc: '2027-06-02T07:00:00Z',
        },
      ],
    });

    second.complete();

    expect(store.spans()).toHaveLength(1);
    expect(store.isSpansLoading()).toBe(false);
  });
});
