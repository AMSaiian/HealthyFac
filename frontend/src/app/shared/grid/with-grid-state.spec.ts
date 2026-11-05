import { TestBed } from '@angular/core/testing';
import { signalStore } from '@ngrx/signals';

import { SortDirection } from '@shared/grid/requests/grid-request';
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
});
