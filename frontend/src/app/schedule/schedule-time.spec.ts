import { type DaySchedule, WorkOrderPriority, WorkOrderStatus } from '@work-orders/work-order';

import { Temporal } from '@shared/time/temporal';

import { followToday, toRow } from './schedule-time';

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
      status: WorkOrderStatus.InProgress,
      priority: WorkOrderPriority.High,
      plannedStartLocal: '2027-06-01T09:00:00',
      plannedStartUtc: '2027-06-01T06:00:00Z',
      estimateMinutes: 60,
      occupancyEndUtc: '2027-06-01T07:20:00Z',
      isDelayed: false,
      rowVersion: 2,
    },
  ],
};

describe('toRow', () => {
  it('toRow_WithDay_MapsBlocksAndBarsToInstants', () => {
    const row = toRow('e1', 'Ann Lee', DAY);

    expect(row.label).toBe('Ann Lee');
    expect(row.blocks[0].start.toString()).toBe('2027-06-01T05:00:00Z');
    expect(row.bars[0].start.toString()).toBe('2027-06-01T06:00:00Z');
    expect(row.bars[0].end.toString()).toBe('2027-06-01T07:20:00Z');
    expect(row.bars[0].colorClass).toBe('hf-status-inProgress');
    expect(row.bars[0].statusKey).toBe('WORK_ORDERS.STATUSES.IN_PROGRESS');
  });
});

describe('followToday', () => {
  const yesterday = Temporal.PlainDate.from('2027-06-01');
  const today = Temporal.PlainDate.from('2027-06-02');

  it('followToday_WhenTheShownDateWasTodayAndTheDayChanged_ReturnsTheNewToday', () => {
    const result = followToday(yesterday, yesterday, today);

    expect(result?.toString()).toBe('2027-06-02');
  });

  it('followToday_WhenAnotherDateIsShown_ReturnsTheShownDate', () => {
    const shown = Temporal.PlainDate.from('2027-06-05');

    const result = followToday(shown, yesterday, today);

    expect(result?.toString()).toBe('2027-06-05');
  });

  it('followToday_WhenTheDayDidNotChange_ReturnsTheShownDate', () => {
    const result = followToday(today, today, today);

    expect(result?.toString()).toBe('2027-06-02');
  });

  it('followToday_WithoutAShownDate_ReturnsNull', () => {
    const result = followToday(null, yesterday, today);

    expect(result).toBeNull();
  });
});
