/** What the paginator binds. A keyset paginator has no page links, no first or last button, and no jump list. */
export interface GridPaginator {
  readonly first: number | null;
  readonly showPageLinks: boolean;
  readonly showFirstLastIcon: boolean;
  readonly showJumpToPageDropdown: boolean;
}

/** What a keyset page event asks for: another page size, the next page, the previous page, or nothing. */
export type KeysetMove = 'limit' | 'next' | 'previous' | 'none';

export interface KeysetPageEvent {
  /** The 0-based page that the paginator moved to. */
  readonly page: number;
  readonly rows: number;
  /** The 0-based page that the store shows. */
  readonly pageIndex: number;
  readonly limit: number;
}

/** Maps a paginator event to a keyset move. Only a step of one page moves; a page size change wins over a step. */
export function toKeysetMove(event: KeysetPageEvent): KeysetMove {
  if (event.rows !== event.limit) {
    return 'limit';
  }
  if (event.page === event.pageIndex + 1) {
    return 'next';
  }
  if (event.page === event.pageIndex - 1) {
    return 'previous';
  }
  return 'none';
}

/**
 * The paginator bindings of a grid. An offset grid (`pageIndex` is `null`) keeps the defaults of the table. A keyset
 * grid hides what it cannot do, and `first` stays `pageIndex * limit`, so the page report reads right.
 */
export function toPaginator(pageIndex: number | null, limit: number): GridPaginator {
  return pageIndex === null
    ? { first: null, showPageLinks: true, showFirstLastIcon: true, showJumpToPageDropdown: false }
    : { first: pageIndex * limit, showPageLinks: false, showFirstLastIcon: false, showJumpToPageDropdown: false };
}
