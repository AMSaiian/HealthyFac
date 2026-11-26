import { type GridColumnState, reorderColumns, toggleColumn, visibleColumns } from './grid-columns';

type TestColumn = 'a' | 'b' | 'c';

const COLUMNS: readonly GridColumnState<TestColumn>[] = [
  { column: 'a', isVisible: true },
  { column: 'b', isVisible: false },
  { column: 'c', isVisible: true },
];

describe('visibleColumns', { tags: ['Persistence'] }, () => {
  it('visibleColumns_WithHiddenColumn_ListsVisibleOnesInOrder', () => {
    const result = visibleColumns(COLUMNS);

    expect(result).toEqual(['a', 'c']);
  });
});

describe('toggleColumn', { tags: ['Persistence'] }, () => {
  it.each([
    ['WhenShown_AppendsColumn', ['a'], 'c', true, ['a', 'c']],
    ['WhenHidden_RemovesColumn', ['a', 'c'], 'a', false, ['c']],
  ] as const)('toggleColumn_%s', (_name, columns, column, isShown, expected) => {
    const result = toggleColumn<TestColumn>([...columns], column, isShown);

    expect(result).toEqual(expected);
  });
});

describe('reorderColumns', { tags: ['Persistence'] }, () => {
  it('reorderColumns_WithHiddenColumn_ReordersOnlyVisibleSlots', () => {
    const result = reorderColumns(COLUMNS, ['c', 'a']);

    expect(result).toEqual([
      { column: 'c', isVisible: true },
      { column: 'b', isVisible: false },
      { column: 'a', isVisible: true },
    ]);
  });
});
