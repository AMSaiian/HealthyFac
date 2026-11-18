export enum SortDirection {
  Ascending = 'asc',
  Descending = 'desc',
}

export interface GridSort<TColumn extends string> {
  readonly column: TColumn;
  readonly direction: SortDirection;
}

/** The grid state of a page: what the user filters, sorts, and pages by. */
export interface GridQueryState<TFilter, TColumn extends string> {
  readonly offset: number;
  readonly limit: number;
  readonly sort: GridSort<TColumn> | null;
  readonly filter: TFilter;
}

/** The body of a backend grid search (`GridRequest<TFilter, TSortColumn>`). */
export interface GridRequestBody<TFilterBody, TColumn extends string> {
  readonly offset: number;
  readonly limit: number;
  readonly sort?: GridSort<TColumn>;
  readonly filter: TFilterBody;
}

/** One page of a backend grid search (`GridResponse<TItem>`). */
export interface GridPage<TItem> {
  readonly items: readonly TItem[];
  readonly totalCount: number;
}

declare const keysetPositionBrand: unique symbol;

/**
 * The position of a row in a keyset grid. The page sends back the `nextPosition` of a page as `after`, and never
 * reads inside it.
 */
export interface KeysetPosition {
  readonly [keysetPositionBrand]: true;
}

/** Marks a value from a response as a keyset position. */
export function toKeysetPosition(value: unknown): KeysetPosition {
  return value as KeysetPosition;
}

/** The body of a backend keyset grid search (`KeysetGridRequest<TFilter, TSortColumn>`). */
export interface KeysetGridRequestBody<TFilterBody, TColumn extends string> {
  readonly limit: number;
  readonly sort?: GridSort<TColumn>;
  readonly filter: TFilterBody;
  readonly after?: KeysetPosition;
}

/** One page of a backend keyset grid search (`KeysetGridResponse<TItem>`). */
export interface KeysetPage<TItem> {
  readonly items: readonly TItem[];
  readonly totalCount: number;
  readonly nextPosition: KeysetPosition | null;
}
