import { type GridColumnState, reorderColumns, toggleColumn, visibleColumns } from './grid-columns';

type TestColumn = 'a' | 'b' | 'c';

const COLUMNS: readonly GridColumnState<TestColumn>[] = [
  { column: 'a', isVisible: true },
  { column: 'b', isVisible: false },
  { column: 'c', isVisible: true },
];

describe('visibleColumns', () => {
  it('visibleColumns_WithHiddenColumn_ListsVisibleOnesInOrder', () => {
    const result = visibleColumns(COLUMNS);

    expect(result).toEqual(['a', 'c']);
  });
});

describe('toggleColumn', () => {
  it('toggleColumn_WhenShown_AppendsColumn', () => {
    const result = toggleColumn<TestColumn>(['a'], 'c', true);

    expect(result).toEqual(['a', 'c']);
  });

  it('toggleColumn_WhenHidden_RemovesColumn', () => {
    const result = toggleColumn<TestColumn>(['a', 'c'], 'a', false);

    expect(result).toEqual(['c']);
  });
});

describe('reorderColumns', () => {
  it('reorderColumns_WithHiddenColumn_ReordersOnlyVisibleSlots', () => {
    const result = reorderColumns(COLUMNS, ['c', 'a']);

    expect(result).toEqual([
      { column: 'c', isVisible: true },
      { column: 'b', isVisible: false },
      { column: 'a', isVisible: true },
    ]);
  });
});
