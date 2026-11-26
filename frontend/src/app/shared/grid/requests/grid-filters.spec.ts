import { Temporal } from '@shared/time/temporal';

import { dateRangeFilter, idSetFilter, plainDateRangeFilter, setFilter, textFilter } from './grid-filters';

const ZONE = 'Europe/Kyiv';
const DAY = Temporal.PlainDate.from('2026-03-29');

describe('grid filters', { tags: ['Persistence'] }, () => {
  it('dateRangeFilter_WithDateRange_ConvertsLocalDaysToHalfOpenUtc', () => {
    const body = dateRangeFilter({ from: DAY, to: DAY }, ZONE);

    expect(body).toEqual({ from: '2026-03-28T22:00:00Z', to: '2026-03-29T21:00:00Z' });
  });

  it('dateRangeFilter_WithOpenBound_OmitsIt', () => {
    const body = dateRangeFilter({ from: DAY, to: null }, ZONE);

    expect(body).toEqual({ from: '2026-03-28T22:00:00Z' });
  });

  it('dateRangeFilter_WithEmptyRange_ReturnsUndefined', () => {
    const body = dateRangeFilter({ from: null, to: null }, ZONE);

    expect(body).toBeUndefined();
  });

  it('plainDateRangeFilter_WithEmptyRange_ReturnsUndefined', () => {
    const body = plainDateRangeFilter({ from: null, to: null });

    expect(body).toBeUndefined();
  });

  it('plainDateRangeFilter_WithOnlyFrom_SendsTheDay', () => {
    const body = plainDateRangeFilter({ from: DAY, to: null });

    expect(body).toEqual({ from: '2026-03-29' });
  });

  it.each([
    ['WithOnlyTo_SendsTheNextDay', '2026-03-11', '2026-03-12'],
    ['WithMonthEnd_RollsToNextMonth', '2026-01-31', '2026-02-01'],
  ])('plainDateRangeFilter_%s', (_name, to, expected) => {
    const body = plainDateRangeFilter({ from: null, to: Temporal.PlainDate.from(to) });

    expect(body).toEqual({ to: expected });
  });

  it('plainDateRangeFilter_WithBothDays_SendsHalfOpenRange', () => {
    const body = plainDateRangeFilter({ from: DAY, to: DAY });

    expect(body).toEqual({ from: '2026-03-29', to: '2026-03-30' });
  });

  it('textFilter_WithEmptyText_ReturnsUndefined', () => {
    const body = textFilter('');

    expect(body).toBeUndefined();
  });

  it('idSetFilter_WithNoIds_ReturnsUndefined', () => {
    const body = idSetFilter([]);

    expect(body).toBeUndefined();
  });

  it('setFilter_WithNoValues_ReturnsUndefined', () => {
    const body = setFilter([]);

    expect(body).toBeUndefined();
  });
});
