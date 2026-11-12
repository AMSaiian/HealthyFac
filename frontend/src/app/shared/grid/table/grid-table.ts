import { computed, inject, type Signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import type { TableColumnReorderEvent, TablePageEvent } from 'primeng/table';
import type { ColumnFilterPassThrough, TablePassThrough } from 'primeng/types/table';

import type { GridSort } from '@shared/grid/requests/grid-request';
import { toggleColumn } from '@shared/grid/table/grid-columns';
import { isFilterValueChanged } from '@shared/grid/table/filter-changes';
import { filterPassThrough, GRID_TOOLTIP_KEYS, tablePassThrough } from '@shared/grid/table/grid-tooltips';
import { isEmptyResult, keepWhileLoading, skeletonRows as skeletonRowCount } from '@shared/grid/table/grid-display';
import { toSortDirection, toSortOrder } from '@shared/grid/table/grid-sort-order';
import { readTableFilters, type TableFilters, toConstraint, toTableFilters } from '@shared/grid/table/table-filters';
import { delayedLoading } from '@shared/loading/delayed-loading';

export interface GridTableOptions<TRow, TColumn extends string, TFilter extends object, TSortColumn extends string> {
  readonly allColumns: readonly TColumn[];
  readonly columns: Signal<readonly TColumn[]>;
  readonly filter: Signal<TFilter>;
  readonly sort: Signal<GridSort<TSortColumn> | null>;
  readonly limit: Signal<number>;
  readonly isLoading: Signal<boolean>;
  readonly rows: Signal<readonly TRow[]>;
  /** `true` once the first result has arrived, so an empty result is not shown before the first load. */
  readonly hasLoaded: Signal<boolean>;
  readonly filterReaders: Readonly<Record<TColumn, (value: unknown) => Partial<TFilter>>>;
  readonly filterWriters: Readonly<Record<TColumn, (filter: TFilter) => unknown>>;
  readonly sortableColumns: readonly string[];
  readonly labelKeys: Readonly<Record<TColumn, string>>;
  readonly filterChange: (patch: Partial<TFilter>) => void;
  readonly sortChange: (sort: GridSort<TSortColumn> | null) => void;
  readonly pageChange: (page: { offset: number; limit: number }) => void;
  readonly columnsChange: (columns: readonly TColumn[]) => void;
  readonly columnsReorder: (columns: readonly TColumn[]) => void;
}

export interface GridTable<TColumn extends string> {
  readonly sortOrder: Signal<number>;
  readonly showSkeleton: Signal<boolean>;
  readonly showEmpty: Signal<boolean>;
  readonly tableFilters: Signal<TableFilters>;
  readonly skeletonRows: Signal<number[]>;
  readonly tableRows: Signal<unknown[]>;
  readonly tableColumns: Signal<TColumn[]>;
  readonly tablePt: Signal<TablePassThrough>;
  readonly filterPt: Signal<ColumnFilterPassThrough>;
  onFilterHidden(column: TColumn, pendingValue: unknown): void;
  onFilterCleared(column: TColumn): void;
  onPage(event: TablePageEvent): void;
  onSort(event: { field?: string | null; order?: number | null }): void;
  onColumnReorder(event: TableColumnReorderEvent): void;
  onColumnToggle(column: TColumn, isVisible: boolean): void;
  isColumnVisible(column: TColumn): boolean;
  columnLabelKey(column: TColumn): string;
  isSortable(column: TColumn): boolean;
}

/**
 * Connects a PrimeNG table to the store of a grid. Each signal reads only its own input, so a sort change does not
 * rebuild the table filters, and a page change does not rebuild the rows. A column filter emits only when its value
 * changed in the store space (the reader output against the filter, lists as sets), so closing a menu without a
 * change sends no request.
 */
export function createGridTable<TRow, TColumn extends string, TFilter extends object, TSortColumn extends string>(
  options: GridTableOptions<TRow, TColumn, TFilter, TSortColumn>,
): GridTable<TColumn> {
  const showSkeleton = delayedLoading(options.isLoading);
  const keptRows = keepWhileLoading(options.rows, options.isLoading);
  const sortOrder = computed(() => toSortOrder(options.sort()?.direction));
  const tableFilters = computed((): TableFilters =>
    toTableFilters(options.allColumns, options.filterWriters, options.filter()),
  );
  const skeletonRows = computed(() => skeletonRowCount(options.limit()));
  const tableRows = computed((): unknown[] => (showSkeleton() ? skeletonRows() : [...keptRows()]));
  const showEmpty = computed(() =>
    isEmptyResult({ rows: options.rows(), isLoading: options.isLoading(), hasLoaded: options.hasLoaded() }),
  );
  const tableColumns = computed(() => [...options.columns()]);
  const labels = toSignal(inject(TranslateService).stream(Object.values(GRID_TOOLTIP_KEYS)), {
    initialValue: {} as Record<string, string>,
  });
  const tablePt = computed(() => tablePassThrough(labels()));
  const filterPt = computed(() => filterPassThrough(labels()));

  function emitIfChanged(column: TColumn, value: unknown): void {
    const patch = readTableFilters([column], options.filterReaders, { [column]: [toConstraint(value)] });
    const filter = options.filter();
    const isChanged = Object.entries(patch).some(([key, patched]) =>
      isFilterValueChanged(filter[key as keyof TFilter], patched),
    );
    if (isChanged) {
      options.filterChange(patch);
    }
  }

  function onFilterHidden(column: TColumn, pendingValue: unknown): void {
    emitIfChanged(column, pendingValue);
  }

  function onFilterCleared(column: TColumn): void {
    emitIfChanged(column, null);
  }

  function onPage(event: TablePageEvent): void {
    options.pageChange({ offset: event.first, limit: event.rows });
  }

  function onSort(event: { field?: string | null; order?: number | null }): void {
    const column = options.sortableColumns.find((candidate) => candidate === event.field) as TSortColumn | undefined;
    options.sortChange(column === undefined ? null : { column, direction: toSortDirection(event.order) });
  }

  function onColumnReorder(event: TableColumnReorderEvent): void {
    options.columnsReorder((event.columns ?? []) as TColumn[]);
  }

  function onColumnToggle(column: TColumn, isVisible: boolean): void {
    options.columnsChange(toggleColumn(options.columns(), column, isVisible));
  }

  return {
    sortOrder,
    showSkeleton,
    showEmpty,
    tableFilters,
    skeletonRows,
    tableRows,
    tableColumns,
    tablePt,
    filterPt,
    onFilterHidden,
    onFilterCleared,
    onPage,
    onSort,
    onColumnReorder,
    onColumnToggle,
    isColumnVisible: (column: TColumn): boolean => options.columns().includes(column),
    columnLabelKey: (column: TColumn): string => options.labelKeys[column],
    isSortable: (column: TColumn): boolean => options.sortableColumns.includes(column),
  };
}
