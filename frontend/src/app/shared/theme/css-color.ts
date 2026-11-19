/**
 * Resolves a CSS token to a color that a canvas can parse. The raw token can be `light-dark(...)`, which a canvas reads
 * as black, so a probe element takes the token as its `color` and the browser gives the computed `rgb(...)`. Call it
 * again with each theme change.
 * @param document The document of the page.
 * @param name The name of the CSS custom property, for example `--p-primary-color`.
 */
export function cssColor(document: Document, name: string): string {
  const view = document.defaultView;
  if (view === null) {
    return '';
  }
  const probe = document.createElement('span');
  probe.style.color = `var(${name})`;
  document.body.appendChild(probe);
  const color = view.getComputedStyle(probe).color;
  probe.remove();
  return color;
}
