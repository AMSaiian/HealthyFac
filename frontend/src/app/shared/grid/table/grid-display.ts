import { linkedSignal, type Signal } from '@angular/core';

export const EMPTY_CELL = '—';
export const MAX_SKELETON_ROWS = 10;

/** The placeholder rows of a loading grid: the page size, at most `MAX_SKELETON_ROWS`. */
export function skeletonRows(limit: number): number[] {
  return Array.from({ length: Math.min(limit, MAX_SKELETON_ROWS) }, (_, index) => index);
}

/**
 * Keeps the last rows while a load runs and the new rows are not there yet, so a grid does not empty before its
 * skeleton shows. A finished load shows its own rows, also an empty list.
 */
export function keepWhileLoading<T>(rows: Signal<readonly T[]>, isLoading: Signal<boolean>): Signal<readonly T[]> {
  return linkedSignal<{ rows: readonly T[]; isLoading: boolean }, readonly T[]>({
    source: () => ({ rows: rows(), isLoading: isLoading() }),
    computation: (source, previous) =>
      source.isLoading && source.rows.length === 0 && previous ? previous.value : source.rows,
  });
}

export interface EmptyResultState {
  readonly rows: readonly unknown[];
  readonly isLoading: boolean;
  readonly hasLoaded: boolean;
}

/**
 * `true` only after a finished load that returned no rows. Before the first result, and during a load, a grid shows no
 * empty state, so it never shows "no rows" while its first load starts.
 */
export function isEmptyResult(state: EmptyResultState): boolean {
  return state.hasLoaded && !state.isLoading && state.rows.length === 0;
}
