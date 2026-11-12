import type { ColumnFilterPassThrough, TablePassThrough } from 'primeng/types/table';

export const GRID_TOOLTIP_KEYS = {
  filter: 'ACTIONS.TOOLTIPS.FILTER',
  clear: 'ACTIONS.TOOLTIPS.CLEAR',
  firstPage: 'ACTIONS.TOOLTIPS.FIRST_PAGE',
  previousPage: 'ACTIONS.TOOLTIPS.PREVIOUS_PAGE',
  nextPage: 'ACTIONS.TOOLTIPS.NEXT_PAGE',
  lastPage: 'ACTIONS.TOOLTIPS.LAST_PAGE',
  rowsPerPage: 'ACTIONS.TOOLTIPS.ROWS_PER_PAGE',
} as const;

type Labels = Readonly<Record<string, string>>;

const titleOf = (labels: Labels, key: string): { title: string } => ({ title: labels[key] ?? key });

/** The titles of the paginator controls that PrimeNG draws. */
export function tablePassThrough(labels: Labels): TablePassThrough {
  return {
    pcPaginator: {
      first: titleOf(labels, GRID_TOOLTIP_KEYS.firstPage),
      prev: titleOf(labels, GRID_TOOLTIP_KEYS.previousPage),
      next: titleOf(labels, GRID_TOOLTIP_KEYS.nextPage),
      last: titleOf(labels, GRID_TOOLTIP_KEYS.lastPage),
      pcRowPerPageDropdown: { root: titleOf(labels, GRID_TOOLTIP_KEYS.rowsPerPage) },
    },
  };
}

/**
 * The titles and the labels of the filter button and the Clear button that PrimeNG draws in a column filter. The
 * button bar justifies its content to the end, so the Clear button sits at the right end of the filter menu. The
 * overlay gets another role: PrimeNG closes a filter menu on an outside click only while one element has the role
 * `dialog`, so the `dialog` role of the overlay would keep every menu open inside a dialog.
 */
export function filterPassThrough(labels: Labels): ColumnFilterPassThrough {
  const attributesOf = (key: string): { title: string; 'aria-label': string } => ({
    ...titleOf(labels, key),
    'aria-label': labels[key] ?? key,
  });
  return {
    pcColumnFilterButton: { root: attributesOf(GRID_TOOLTIP_KEYS.filter) },
    pcFilterClearButton: { root: attributesOf(GRID_TOOLTIP_KEYS.clear) },
    filterButtonBar: { class: 'justify-end' },
    filterOverlay: { role: 'group' },
  };
}
