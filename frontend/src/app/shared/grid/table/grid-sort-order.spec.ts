import { SortDirection } from '@shared/grid/requests/grid-request';
import { toSortDirection, toSortOrder } from './grid-sort-order';

describe('grid sort order', () => {
  it.each([
    { name: 'WithDescending_ReturnsMinusOne', direction: SortDirection.Descending, expected: -1 },
    { name: 'WithoutDirection_ReturnsAscending', direction: undefined, expected: 1 },
  ])('toSortOrder_$name', ({ direction, expected }) => {
    const order = toSortOrder(direction);

    expect(order).toBe(expected);
  });

  it.each([
    { name: 'WithMinusOne_ReturnsDescending', order: -1, expected: SortDirection.Descending },
    { name: 'WithoutOrder_ReturnsAscending', order: null, expected: SortDirection.Ascending },
  ])('toSortDirection_$name', ({ order, expected }) => {
    const direction = toSortDirection(order);

    expect(direction).toBe(expected);
  });
});
