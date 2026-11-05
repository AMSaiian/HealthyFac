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
