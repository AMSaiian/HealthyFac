import { isFilterChanged } from '@shared/grid/table/filter-changes';

describe('isFilterChanged', () => {
  it('isFilterChanged_WithSameValuesInOtherOrder_ReturnsFalse', () => {
    const applied = ['low', 'high'];
    const pending = ['high', 'low'];

    const result = isFilterChanged(applied, pending);

    expect(result).toBe(false);
  });

  it('isFilterChanged_WithAddedValue_ReturnsTrue', () => {
    const applied = ['low'];
    const pending = ['low', 'high'];

    const result = isFilterChanged(applied, pending);

    expect(result).toBe(true);
  });

  it('isFilterChanged_WithRemovedValue_ReturnsTrue', () => {
    const applied = ['low', 'high'];
    const pending = ['low'];

    const result = isFilterChanged(applied, pending);

    expect(result).toBe(true);
  });

  it('isFilterChanged_WithTwoEmptyLists_ReturnsFalse', () => {
    const applied: string[] = [];
    const pending: string[] = [];

    const result = isFilterChanged(applied, pending);

    expect(result).toBe(false);
  });
});
