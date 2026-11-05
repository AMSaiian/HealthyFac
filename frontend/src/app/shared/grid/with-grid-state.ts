import { computed, type Signal } from '@angular/core';
import {
  type EmptyFeatureResult,
  patchState,
  type SignalStoreFeature,
  signalStoreFeature,
  withComputed,
  withMethods,
  withState,
} from '@ngrx/signals';

import { type GridColumnState, reorderColumns, visibleColumns } from '@shared/grid/table/grid-columns';
import type { GridSort } from '@shared/grid/requests/grid-request';

export interface GridState<TFilter, TSortColumn extends string, TColumn extends string> {
  offset: number;
  limit: number;
  sort: GridSort<TSortColumn> | null;
  filter: TFilter;
  columns: readonly GridColumnState<TColumn>[];
}

export interface GridStateInit<TFilter, TSortColumn extends string, TColumn extends string> {
  readonly limit: number;
  readonly sort: GridSort<TSortColumn> | null;
  readonly filter: TFilter;
  readonly columns: readonly TColumn[];
}

function initialGridState<TFilter extends object, TSortColumn extends string, TColumn extends string>(
  initial: GridStateInit<TFilter, TSortColumn, TColumn>,
): GridState<TFilter, TSortColumn, TColumn> {
  return {
    offset: 0,
    limit: initial.limit,
    sort: initial.sort,
    filter: initial.filter,
    columns: initial.columns.map((column) => ({ column, isVisible: true })),
  };
}

/**
 * The state of a server-side grid: page, sort, filter, and columns. A filter or sort change resets the offset to 0.
 * A grid store composes it and adds its own resource and actions.
 */
export function withGridState<TFilter extends object, TSortColumn extends string, TColumn extends string>(
  initial: GridStateInit<TFilter, TSortColumn, TColumn>,
): SignalStoreFeature<
  EmptyFeatureResult,
  {
    state: GridState<TFilter, TSortColumn, TColumn>;
    props: { visibleColumns: Signal<TColumn[]> };
    methods: {
      setFilter: (patch: Partial<TFilter>) => void;
      setSort: (sort: GridSort<TSortColumn> | null) => void;
      setPage: (offset: number, limit: number) => void;
      setColumns: (visible: readonly TColumn[]) => void;
      reorderColumns: (order: readonly TColumn[]) => void;
      resetGrid: () => void;
    };
  }
> {
  return signalStoreFeature(
    withState<GridState<TFilter, TSortColumn, TColumn>>(initialGridState(initial)),
    withComputed((store) => ({
      visibleColumns: computed(() => visibleColumns(store.columns())),
    })),
    withMethods((store) => ({
      setFilter(patch: Partial<TFilter>): void {
        patchState(store, (state) => ({ filter: { ...state.filter, ...patch }, offset: 0 }));
      },
      setSort(sort: GridSort<TSortColumn> | null): void {
        patchState(store, { sort, offset: 0 });
      },
      setPage(offset: number, limit: number): void {
        patchState(store, { offset, limit });
      },
      setColumns(visible: readonly TColumn[]): void {
        patchState(store, (state) => ({
          columns: state.columns.map((entry) => ({ column: entry.column, isVisible: visible.includes(entry.column) })),
        }));
      },
      reorderColumns(order: readonly TColumn[]): void {
        patchState(store, (state) => ({ columns: reorderColumns(state.columns, order) }));
      },
      resetGrid(): void {
        patchState(store, initialGridState(initial));
      },
    })),
  );
}
