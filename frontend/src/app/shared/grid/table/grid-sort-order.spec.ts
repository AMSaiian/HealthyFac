import { SortDirection } from '@shared/grid/requests/grid-request';
import { toSortDirection, toSortOrder } from './grid-sort-order';

describe('grid sort order', () => {
  it('toSortOrder_WithDescending_ReturnsMinusOne', () => {
    const order = toSortOrder(SortDirection.Descending);

    expect(order).toBe(-1);
  });

  it('toSortOrder_WithoutDirection_ReturnsAscending', () => {
    const order = toSortOrder(undefined);

    expect(order).toBe(1);
  });

  it('toSortDirection_WithMinusOne_ReturnsDescending', () => {
    const direction = toSortDirection(-1);

    expect(direction).toBe(SortDirection.Descending);
  });

  it('toSortDirection_WithoutOrder_ReturnsAscending', () => {
    const direction = toSortDirection(null);

    expect(direction).toBe(SortDirection.Ascending);
  });
});
