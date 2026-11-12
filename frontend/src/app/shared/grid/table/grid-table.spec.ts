import { signal, type WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { type Mock, vi } from 'vitest';

import { type GridSort, SortDirection } from '@shared/grid/requests/grid-request';
import { asList, asRange, asText } from '@shared/grid/table/table-filters';
import { EMPTY_RANGE, isRangeEmpty, type PlainDateRange } from '@shared/time/plain-date-range';

import { createGridTable, type GridTable } from './grid-table';

type Column = 'name' | 'serial' | 'tags' | 'period';

interface Filter {
  readonly name: string;
  readonly serial: string;
  readonly tags: readonly string[];
  readonly period: PlainDateRange;
}

const LABEL_KEYS: Record<Column, string> = {
  name: 'GRID.NAME',
  serial: 'GRID.SERIAL',
  tags: 'GRID.TAGS',
  period: 'GRID.PERIOD',
};
const INITIAL_FILTER: Filter = { name: 'Lathe', serial: '', tags: ['a', 'b'], period: EMPTY_RANGE };

interface GridTableHarness {
  readonly table: GridTable<Column>;
  readonly sort: WritableSignal<GridSort<'name'> | null>;
  readonly filterChange: Mock;
}

function setup(nameWriter: (current: Filter) => unknown = (current) => current.name || null): GridTableHarness {
  const filter = signal<Filter>(INITIAL_FILTER);
  const sort = signal<GridSort<'name'> | null>(null);
  const filterChange = vi.fn();
  TestBed.configureTestingModule({
    providers: [{ provide: TranslateService, useValue: { stream: () => of({}) } }],
  });
  const table = TestBed.runInInjectionContext(() =>
    createGridTable({
      allColumns: ['name', 'serial', 'tags', 'period'],
      columns: signal<readonly Column[]>(['name', 'serial', 'tags', 'period']),
      filter,
      sort,
      limit: signal(25),
      isLoading: signal(false),
      rows: signal<readonly unknown[]>([]),
      hasLoaded: signal(true),
      filterReaders: {
        name: (value) => ({ name: asText(value) }),
        serial: (value) => ({ serial: asText(value) }),
        tags: (value) => ({ tags: asList<string>(value) }),
        period: (value) => ({ period: asRange(value) }),
      },
      filterWriters: {
        name: nameWriter,
        serial: (current) => current.serial || null,
        tags: (current) => (current.tags.length > 0 ? [...current.tags] : null),
        period: (current) => (isRangeEmpty(current.period) ? null : current.period),
      },
      sortableColumns: ['name'],
      labelKeys: LABEL_KEYS,
      filterChange,
      sortChange: vi.fn(),
      pageChange: vi.fn(),
      columnsChange: vi.fn(),
      columnsReorder: vi.fn(),
    }),
  );

  return { table, sort, filterChange };
}

describe('createGridTable', () => {
  it('onFilterHidden_WithUnchangedValue_EmitsNothing', () => {
    const { table, filterChange } = setup();

    table.onFilterHidden('name', 'Lathe');

    expect(filterChange).not.toHaveBeenCalled();
  });

  it('onFilterHidden_WithChangedValue_EmitsOnce', () => {
    const { table, filterChange } = setup();

    table.onFilterHidden('name', 'Press');

    expect(filterChange).toHaveBeenCalledTimes(1);
    expect(filterChange).toHaveBeenCalledWith({ name: 'Press' });
  });

  it.each([
    ['WithSameValuesInOtherOrder', 'tags', ['b', 'a']],
    ['WithOnlyAddedWhiteSpace', 'name', '  Lathe '],
    ['WithOnlyWhiteSpaceOverAnEmptyValue', 'serial', '   '],
    ['WithAClearedDateRange', 'period', { from: null, to: null }],
  ] as const)('onFilterHidden_%s_EmitsNothing', (_name, column, pending) => {
    const { table, filterChange } = setup();

    table.onFilterHidden(column, pending);

    expect(filterChange).not.toHaveBeenCalled();
  });

  it('onFilterCleared_WithEmptyFilter_EmitsNothing', () => {
    const { table, filterChange } = setup();

    table.onFilterCleared('serial');

    expect(filterChange).not.toHaveBeenCalled();
  });

  it('filterPt_HoldsTheEndAlignedClassOnTheFilterButtonBar', () => {
    const { table } = setup();

    expect(table.filterPt()).toMatchObject({ filterButtonBar: { class: 'justify-end' } });
  });

  it('sortChange_DoesNotRecomputeTableFilters', () => {
    const nameWriter = vi.fn((current: Filter): unknown => current.name || null);
    const { table, sort } = setup(nameWriter);
    table.tableFilters();

    sort.set({ column: 'name', direction: SortDirection.Ascending });
    table.tableFilters();

    expect(nameWriter).toHaveBeenCalledTimes(1);
  });
});
