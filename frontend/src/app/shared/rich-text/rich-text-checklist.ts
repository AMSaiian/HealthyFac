const CHECKLIST_ITEM = /<li data-list="(checked|unchecked)"/g;

const CHECKED = 'checked';
const UNCHECKED = 'unchecked';

/**
 * Switches the state of one checklist item in a stored rich text. Quill 2 writes a checklist item as
 * `<li data-list="checked">` or `<li data-list="unchecked">`; the function counts only these items, in document order.
 * Only the attribute value changes, so the rest of the HTML stays as it was. An index without an item returns the same
 * text.
 */
export function toggleChecklistItem(html: string, index: number): string {
  let position = -1;

  return html.replace(CHECKLIST_ITEM, (match, state: string) => {
    position++;
    return position === index ? `<li data-list="${state === CHECKED ? UNCHECKED : CHECKED}"` : match;
  });
}

export const CHECKED_CLASS = 'hf-check-checked';
export const UNCHECKED_CLASS = 'hf-check-unchecked';

/**
 * Adds a class to each checklist item for the display. The Angular sanitizer of `[innerHTML]` removes `data-*`
 * attributes but keeps `class`, so the view styles the items by these classes.
 */
export function markChecklistItems(html: string): string {
  return html.replace(CHECKLIST_ITEM, (_match, state: string) =>
    state === CHECKED
      ? `<li data-list="${state}" class="${CHECKED_CLASS}"`
      : `<li data-list="${state}" class="${UNCHECKED_CLASS}"`,
  );
}
