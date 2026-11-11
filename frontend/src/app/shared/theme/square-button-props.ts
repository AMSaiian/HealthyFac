import type { ButtonDirectiveOptions } from 'primeng/types/button';
import type { ConfirmDialogPassThrough } from 'primeng/types/confirmdialog';
import type { TableFilterButtonPropsOptions } from 'primeng/types/table';

/** The column filter button and its clear button without the round shape that PrimeNG sets by default. */
export const SQUARE_FILTER_BUTTON_PROPS: TableFilterButtonPropsOptions = {
  filter: { rounded: false },
  inline: { clear: { rounded: false } },
};

/**
 * The close button of a dialog or a drawer without the round shape. It replaces the default props of PrimeNG, so it
 * repeats the severity and the variant.
 */
export const SQUARE_CLOSE_BUTTON_PROPS: ButtonDirectiveOptions = {
  severity: 'secondary',
  variant: 'text',
  rounded: false,
};

/**
 * The close button of the confirm dialog without the round shape. `p-confirmdialog` has no `closeButtonProps`, so the
 * pass-through sets the radius of a labelled button on the button itself.
 */
export const SQUARE_CONFIRM_DIALOG_PT: ConfirmDialogPassThrough = {
  pcCloseButton: { root: { style: { borderRadius: 'var(--p-button-border-radius)' } } },
};
