import type { TableFilterButtonPropsOptions } from 'primeng/types/table';

/**
 * The column filter button and its clear buttons without the round shape that PrimeNG sets by default. The popover
 * Clear button is filled secondary, so the grid filter menus have one primary action only.
 */
export const SQUARE_FILTER_BUTTON_PROPS: TableFilterButtonPropsOptions = {
  filter: { rounded: false },
  inline: { clear: { rounded: false } },
  popover: { clear: { severity: 'secondary', variant: undefined, rounded: false } },
};

/**
 * Hides the close cross of a dialog. Keep `closable` on: PrimeNG binds the Escape key and the backdrop dismiss only when
 * `closable` is true, so `[closable]="false"` would turn them off too.
 */
export const HIDDEN_CLOSE_BUTTON_PT = {
  pcCloseButton: { root: { class: 'hidden' } },
  footer: { class: 'pt-4' },
} as const;
