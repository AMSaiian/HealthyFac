import { Temporal } from '@shared/time/temporal';

import { type DaySchedule, WorkOrderPriority, WorkOrderStatus } from '@work-orders/work-order';

import { firstFreeStart, type PrefillDay, toPrefillDay } from './slot-prefill';

const ZONE = 'Europe/Kyiv';

function at(local: string): Temporal.Instant {
  return Temporal.PlainDateTime.from(local).toZonedDateTime(ZONE, { disambiguation: 'compatible' }).toInstant();
}

function block(start: string, end: string): { start: Temporal.Instant; end: Temporal.Instant } {
  return { start: at(start), end: at(end) };
}

function localOf(instant: Temporal.Instant | null): string | null {
  return instant === null
    ? null
    : instant.toZonedDateTimeISO(ZONE).toPlainDateTime().toString({ smallestUnit: 'minute' });
}

const DAY: PrefillDay = {
  blocks: [block('2027-06-01T08:00', '2027-06-01T12:00'), block('2027-06-01T13:00', '2027-06-01T17:00')],
  occupancies: [],
};

describe('firstFreeStart', () => {
  it('firstFreeStart_WithFreeDayInThePast_GivesTheStartOfTheFirstBlock', () => {
    const start = firstFreeStart(DAY, 60, at('2027-05-31T10:00'));

    expect(localOf(start)).toBe('2027-06-01T08:00');
  });

  it('firstFreeStart_WithNowInsideABlock_RoundsUpToTheGrid', () => {
    const start = firstFreeStart(DAY, 60, at('2027-06-01T09:02'));

    expect(localOf(start)).toBe('2027-06-01T09:05');
  });

  it('firstFreeStart_WithNowAfterLunchStart_GivesTheNextBlockAfterTheGap', () => {
    const start = firstFreeStart(DAY, 60, at('2027-06-01T11:30'));

    expect(localOf(start)).toBe('2027-06-01T13:00');
  });

  it('firstFreeStart_WithEstimateThatDoesNotFitTheRestOfTheBlock_SkipsToTheNextBlock', () => {
    const start = firstFreeStart(DAY, 120, at('2027-06-01T10:30'));

    expect(localOf(start)).toBe('2027-06-01T13:00');
  });

  it('firstFreeStart_WithOccupancyExtendedByLateCompletion_StartsAfterItsEnd', () => {
    const day: PrefillDay = {
      ...DAY,
      occupancies: [{ start: at('2027-06-01T08:00'), end: at('2027-06-01T09:23') }],
    };

    const start = firstFreeStart(day, 60, at('2027-06-01T07:00'));

    expect(localOf(start)).toBe('2027-06-01T09:25');
  });

  it('firstFreeStart_WithSlotTouchingAnOccupancyEnd_StartsExactlyThere', () => {
    const day: PrefillDay = {
      ...DAY,
      occupancies: [{ start: at('2027-06-01T08:00'), end: at('2027-06-01T09:00') }],
    };

    const start = firstFreeStart(day, 60, at('2027-06-01T07:00'));

    expect(localOf(start)).toBe('2027-06-01T09:00');
  });

  it('firstFreeStart_WithAFullDay_GivesNull', () => {
    const day: PrefillDay = {
      ...DAY,
      occupancies: [
        { start: at('2027-06-01T08:00'), end: at('2027-06-01T12:00') },
        { start: at('2027-06-01T13:00'), end: at('2027-06-01T17:00') },
      ],
    };

    const start = firstFreeStart(day, 60, at('2027-06-01T07:00'));

    expect(start).toBeNull();
  });

  it('firstFreeStart_WithNowAfterTheLastBlock_GivesNull', () => {
    const start = firstFreeStart(DAY, 60, at('2027-06-01T16:30'));

    expect(start).toBeNull();
  });

  it('firstFreeStart_OnTheSpringChangeDay_UsesRealInstants', () => {
    const day: PrefillDay = {
      blocks: [block('2027-03-28T02:00', '2027-03-28T05:00')],
      occupancies: [],
    };

    const start = firstFreeStart(day, 60, at('2027-03-28T01:00'));

    expect(start?.toString()).toBe('2027-03-28T00:00:00Z');
    expect(localOf(start)).toBe('2027-03-28T02:00');
  });

  it('firstFreeStart_OnTheSpringChangeDayWithEstimateOverTheShortBlock_GivesNull', () => {
    const day: PrefillDay = {
      blocks: [block('2027-03-28T02:00', '2027-03-28T05:00')],
      occupancies: [{ start: at('2027-03-28T02:00'), end: at('2027-03-28T02:55') }],
    };

    const start = firstFreeStart(day, 70, at('2027-03-28T01:00'));

    expect(start).toBeNull();
  });

  it('firstFreeStart_OnTheAutumnChangeDay_FitsTheRepeatedHour', () => {
    const day: PrefillDay = {
      blocks: [
        {
          start: Temporal.Instant.from('2027-10-30T23:00:00Z'),
          end: Temporal.Instant.from('2027-10-31T02:00:00Z'),
        },
      ],
      occupancies: [
        { start: Temporal.Instant.from('2027-10-30T23:00:00Z'), end: Temporal.Instant.from('2027-10-31T00:00:00Z') },
      ],
    };

    const start = firstFreeStart(day, 120, Temporal.Instant.from('2027-10-30T20:00:00Z'));

    expect(start?.toString()).toBe('2027-10-31T00:00:00Z');
  });
});

describe('toPrefillDay', () => {
  const workOrder = (id: string, start: string, end: string): DaySchedule['workOrders'][number] => ({
    id,
    title: id,
    status: WorkOrderStatus.Staged,
    priority: WorkOrderPriority.Low,
    plannedStartLocal: '2027-06-01T09:00:00',
    plannedStartUtc: start,
    estimateMinutes: 60,
    occupancyEndUtc: end,
    isDelayed: false,
    rowVersion: 1,
  });
  const schedule: DaySchedule = {
    date: '2027-06-01',
    timeZoneId: ZONE,
    source: 'template',
    blocks: [
      { startLocal: '08:00', endLocal: '12:00', startUtc: '2027-06-01T05:00:00Z', endUtc: '2027-06-01T09:00:00Z' },
    ],
    workOrders: [
      workOrder('other', '2027-06-01T05:00:00Z', '2027-06-01T06:00:00Z'),
      workOrder('moving', '2027-06-01T06:00:00Z', '2027-06-01T07:00:00Z'),
    ],
  };

  it('toPrefillDay_WithTheMovedWorkOrder_LeavesItsOldSlotFree', () => {
    const start = firstFreeStart(toPrefillDay(schedule, 'moving'), 60, at('2027-06-01T07:00'));

    expect(localOf(start)).toBe('2027-06-01T09:00');
  });

  it('toPrefillDay_WithoutAnExcludedWorkOrder_CountsEveryWorkOrder', () => {
    const start = firstFreeStart(toPrefillDay(schedule), 60, at('2027-06-01T07:00'));

    expect(localOf(start)).toBe('2027-06-01T10:00');
  });
});
