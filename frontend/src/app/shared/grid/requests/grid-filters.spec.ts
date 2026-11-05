import { Temporal } from '@shared/time/temporal';

import { dateRangeFilter, idSetFilter, setFilter, textFilter } from './grid-filters';

const ZONE = 'Europe/Kyiv';
const DAY = Temporal.PlainDate.from('2026-03-29');

describe('grid filters', () => {
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
