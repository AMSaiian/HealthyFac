import { isFilterChanged } from '@shared/grid/table/filter-changes';

describe('isFilterChanged', { tags: ['Persistence'] }, () => {
  it.each([
    ['SameValuesInOtherOrder_ReturnsFalse', ['low', 'high'], ['high', 'low'], false],
    ['AddedValue_ReturnsTrue', ['low'], ['low', 'high'], true],
    ['RemovedValue_ReturnsTrue', ['low', 'high'], ['low'], true],
    ['TwoEmptyLists_ReturnsFalse', [], [], false],
  ])('isFilterChanged_With%s', (_name, applied, pending, expected) => {
    const result = isFilterChanged(applied, pending);

    expect(result).toBe(expected);
  });
});
