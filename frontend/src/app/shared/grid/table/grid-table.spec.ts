import { signal, type WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import type { Table } from 'primeng/table';
import { type Mock, vi } from 'vitest';

import { type GridSort, SortDirection } from '@shared/grid/requests/grid-request';
import { asList, asRange, asText } from '@shared/grid/table/table-filters';
import { EMPTY_RANGE, isRangeEmpty, type PlainDateRange } from '@shared/time/plain-date-range';

import { createGridTable, type GridTable, resetColumnWidths, TEXT_COLUMN_WIDTH } from './grid-table';

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
  readonly pageChange: Mock;
  readonly columns: WritableSignal<readonly Column[]>;
}

const MIN_WIDTH = '64rem';

interface FakeTable {
  readonly table: Table;
  readonly destroyStyleElement: Mock;
  readonly element: { style: { width: string; minWidth: string } };
}

function fakeTable(): FakeTable {
  const element = { style: { width: '90rem', minWidth: '90rem' } };
  const destroyStyleElement = vi.fn();
  const table = {
    destroyStyleElement,
    tableViewChild: () => ({ nativeElement: element }),
    tableStyle: () => ({ 'min-width': MIN_WIDTH }),
  } as unknown as Table;
  return { table, destroyStyleElement, element };
}

function setup(
  nameWriter: (current: Filter) => unknown = (current) => current.name || null,
  table: Table | undefined = undefined,
): GridTableHarness {
  const columns = signal<readonly Column[]>(['name', 'serial', 'tags', 'period']);
  const filter = signal<Filter>(INITIAL_FILTER);
  const sort = signal<GridSort<'name'> | null>(null);
  const filterChange = vi.fn();
  const pageChange = vi.fn();
  TestBed.configureTestingModule({
    providers: [{ provide: TranslateService, useValue: { stream: () => of({}) } }],
  });
  const gridTable = TestBed.runInInjectionContext(() =>
    createGridTable({
      allColumns: ['name', 'serial', 'tags', 'period'],
      columns,
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
      pageChange,
      table: signal(table),
      textColumns: ['name'],
      columnsChange: vi.fn(),
      columnsReorder: vi.fn(),
    }),
  );

  return { table: gridTable, sort, filterChange, pageChange, columns };
}

describe('createGridTable', () => {
  it('columns_WhenTheColumnsChange_ResetsTheWidths', () => {
    const { table, destroyStyleElement, element } = fakeTable();
    const { columns } = setup(undefined, table);
    TestBed.tick();

    columns.set(['name', 'serial']);
    TestBed.tick();

    expect(destroyStyleElement).toHaveBeenCalledTimes(1);
    expect(element.style.width).toBe('');
    expect(element.style.minWidth).toBe(MIN_WIDTH);
  });

  it('columns_OnTheFirstRun_KeepsTheWidths', () => {
    const { table, destroyStyleElement, element } = fakeTable();
    setup(undefined, table);

    TestBed.tick();

    expect(destroyStyleElement).not.toHaveBeenCalled();
    expect(element.style.width).toBe('90rem');
  });

  it('columnWidth_ForATextColumn_ReturnsTheTextWidth', () => {
    const { table } = setup();

    expect(table.columnWidth('name')).toBe(TEXT_COLUMN_WIDTH);
    expect(table.columnWidth('serial')).toBeNull();
  });

  it('resetColumnWidths_WithoutATable_DoesNothing', () => {
    expect(() => resetColumnWidths(undefined)).not.toThrow();
  });

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

describe('createGridTable keyset paging', () => {
  interface KeysetHarness {
    readonly table: GridTable<'name'>;
    readonly next: Mock;
    readonly previous: Mock;
    readonly limitChange: Mock;
    readonly pageChange: Mock;
  }

  function setupKeyset(pageIndex: number): KeysetHarness {
    const next = vi.fn();
    const previous = vi.fn();
    const limitChange = vi.fn();
    const pageChange = vi.fn();
    TestBed.configureTestingModule({
      providers: [{ provide: TranslateService, useValue: { stream: () => of({}) } }],
    });
    const table = TestBed.runInInjectionContext(() =>
      createGridTable<unknown, 'name', { readonly name: string }, 'name'>({
        allColumns: ['name'],
        columns: signal<readonly 'name'[]>(['name']),
        filter: signal({ name: '' }),
        sort: signal<GridSort<'name'> | null>(null),
        limit: signal(25),
        isLoading: signal(false),
        rows: signal<readonly unknown[]>([]),
        hasLoaded: signal(true),
        filterReaders: { name: (value) => ({ name: asText(value) }) },
        filterWriters: { name: (current) => current.name || null },
        sortableColumns: ['name'],
        labelKeys: { name: 'GRID.NAME' },
        filterChange: vi.fn(),
        sortChange: vi.fn(),
        pageChange,
        keyset: { pageIndex: signal(pageIndex), next, previous, limitChange },
        table: signal(undefined),
        textColumns: [],
        columnsChange: vi.fn(),
        columnsReorder: vi.fn(),
      }),
    );
    return { table, next, previous, limitChange, pageChange };
  }

  it('onPage_ToThePageAfterTheCurrentOne_CallsNext', () => {
    const { table, next, pageChange } = setupKeyset(1);

    table.onPage({ first: 50, rows: 25 });

    expect(next).toHaveBeenCalledTimes(1);
    expect(pageChange).not.toHaveBeenCalled();
  });

  it('onPage_ToThePageBeforeTheCurrentOne_CallsPrevious', () => {
    const { table, previous } = setupKeyset(1);

    table.onPage({ first: 0, rows: 25 });

    expect(previous).toHaveBeenCalledTimes(1);
  });

  it('onPage_WithANewPageSize_CallsLimitChangeWithTheRows', () => {
    const { table, limitChange, next } = setupKeyset(2);

    table.onPage({ first: 0, rows: 50 });

    expect(limitChange).toHaveBeenCalledWith(50);
    expect(next).not.toHaveBeenCalled();
  });

  it('paginator_OnPageThree_HoldsTheReportOffsetAndNoPageLinks', () => {
    const { table } = setupKeyset(3);

    expect(table.paginator()).toEqual({
      first: 75,
      showPageLinks: false,
      showFirstLastIcon: false,
      showJumpToPageDropdown: false,
    });
  });

  it('onPage_WithoutKeysetOptions_KeepsTheOffsetMapping', () => {
    const { table, pageChange } = setup();

    table.onPage({ first: 50, rows: 25 });

    expect(pageChange).toHaveBeenCalledWith({ offset: 50, limit: 25 });
  });
});
