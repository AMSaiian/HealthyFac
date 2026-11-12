/** The overlay panels that PrimeNG appends while they are open. Escape closes the panel first, then the dialog. */
const OPEN_PANEL_SELECTOR = [
  '.p-select-overlay',
  '.p-multiselect-overlay',
  '.p-datatable-filter-overlay',
  '.p-menu-overlay',
  '.p-popover',
  '.p-datepicker-panel',
  '.p-autocomplete-overlay',
  '.p-confirmpopup',
].join(',');

const DIALOG_SELECTOR = '.p-dialog';

/** Tells whether an overlay panel is open, so an Escape belongs to the panel. */
export function hasOpenPanel(doc: Document): boolean {
  return doc.querySelector(OPEN_PANEL_SELECTOR) !== null;
}

/** Tells whether no other open dialog (a confirmation, for example) stands above the container. */
export function isDialogTopmost(container: HTMLElement, doc: Document): boolean {
  const zIndexOf = (element: Element): number => Number.parseInt((element as HTMLElement).style.zIndex, 10) || 0;
  return [...doc.querySelectorAll(DIALOG_SELECTOR)].every(
    (dialog) => dialog === container || zIndexOf(dialog) < zIndexOf(container),
  );
}
