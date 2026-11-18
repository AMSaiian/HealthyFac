import { TestBed } from '@angular/core/testing';
import { signalStore } from '@ngrx/signals';

import { SortDirection, toKeysetPosition } from '@shared/grid/requests/grid-request';
import { withGridState } from './with-grid-state';

interface TestFilter {
  readonly name: string;
}

type TestColumn = 'a' | 'b' | 'c';

const PAGE_OFFSET = 50;
const PAGE_LIMIT = 10;

const TestStore = signalStore(
  withGridState<TestFilter, 'name', TestColumn>({
    limit: 25,
    sort: { column: 'name', direction: SortDirection.Descending },
    filter: { name: '' },
    columns: ['a', 'b', 'c'],
  }),
);

function setup(): InstanceType<typeof TestStore> {
  TestBed.configureTestingModule({ providers: [TestStore] });
  return TestBed.inject(TestStore);
}

describe('withGridState', () => {
  it('setFilter_Always_ResetsOffset', () => {
    const store = setup();
    store.setPage(PAGE_OFFSET, PAGE_LIMIT);

    store.setFilter({ name: 'la' });

    expect(store.offset()).toBe(0);
    expect(store.limit()).toBe(PAGE_LIMIT);
    expect(store.filter()).toEqual({ name: 'la' });
  });

  it('setSort_Always_ResetsOffset', () => {
    const store = setup();
    store.setPage(PAGE_OFFSET, PAGE_LIMIT);

    store.setSort(null);

    expect(store.offset()).toBe(0);
    expect(store.sort()).toBeNull();
  });

  it('setPage_Always_KeepsFilter', () => {
    const store = setup();
    store.setFilter({ name: 'la' });

    store.setPage(PAGE_OFFSET, PAGE_LIMIT);

    expect(store.offset()).toBe(PAGE_OFFSET);
    expect(store.filter()).toEqual({ name: 'la' });
  });

  it('setColumns_WithSubset_HidesOtherColumns', () => {
    const store = setup();

    store.setColumns(['a', 'c']);

    expect(store.visibleColumns()).toEqual(['a', 'c']);
  });

  it('reorderColumns_WithHiddenColumn_KeepsHiddenColumnInPlace', () => {
    const store = setup();
    store.setColumns(['a', 'c']);

    store.reorderColumns(['c', 'a']);

    expect(store.columns()).toEqual([
      { column: 'c', isVisible: true },
      { column: 'b', isVisible: false },
      { column: 'a', isVisible: true },
    ]);
  });

  it('resetGrid_AfterEveryChange_RestoresInitialState', () => {
    const store = setup();
    store.setFilter({ name: 'la' });
    store.setPage(PAGE_OFFSET, PAGE_LIMIT);
    store.setSort(null);
    store.setColumns(['b']);
    store.reorderColumns(['c', 'b', 'a']);

    store.resetGrid();

    expect(store.offset()).toBe(0);
    expect(store.limit()).toBe(25);
    expect(store.sort()).toEqual({ column: 'name', direction: SortDirection.Descending });
    expect(store.filter()).toEqual({ name: '' });
    expect(store.visibleColumns()).toEqual(['a', 'b', 'c']);
  });
});

describe('withGridState keyset paging', () => {
  const FIRST = toKeysetPosition({ sortValue: 1, id: 'a' });
  const SECOND = toKeysetPosition({ sortValue: 2, id: 'b' });

  it('nextPage_Twice_PushesPositionsAndRaisesThePageIndex', () => {
    const store = setup();

    store.nextPage(FIRST);
    store.nextPage(SECOND);

    expect(store.positions()).toEqual([FIRST, SECOND]);
    expect(store.pageIndex()).toBe(2);
    expect(store.after()).toBe(SECOND);
  });

  it('previousPage_AfterTwoNextPages_PopsTheLastPosition', () => {
    const store = setup();
    store.nextPage(FIRST);
    store.nextPage(SECOND);

    store.previousPage();

    expect(store.positions()).toEqual([FIRST]);
    expect(store.pageIndex()).toBe(1);
  });

  it('previousPage_OnTheFirstPage_StaysOnTheFirstPage', () => {
    const store = setup();

    store.previousPage();

    expect(store.pageIndex()).toBe(0);
    expect(store.after()).toBeNull();
  });

  it.each([
    ['setFilter', (store: InstanceType<typeof TestStore>) => store.setFilter({ name: 'la' })],
    ['setSort', (store: InstanceType<typeof TestStore>) => store.setSort(null)],
    ['setLimit', (store: InstanceType<typeof TestStore>) => store.setLimit(PAGE_LIMIT)],
  ])('%s_OnALaterPage_ClearsThePositions', (_name, change) => {
    const store = setup();
    store.nextPage(FIRST);

    change(store);

    expect(store.positions()).toEqual([]);
    expect(store.pageIndex()).toBe(0);
  });

  it('setLimit_Always_KeepsTheFilterAndSetsTheLimit', () => {
    const store = setup();
    store.setFilter({ name: 'la' });

    store.setLimit(PAGE_LIMIT);

    expect(store.limit()).toBe(PAGE_LIMIT);
    expect(store.filter()).toEqual({ name: 'la' });
  });

  it('resetGrid_AfterNextPage_ClearsThePositions', () => {
    const store = setup();
    store.nextPage(FIRST);

    store.resetGrid();

    expect(store.positions()).toEqual([]);
  });
});
