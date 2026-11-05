/** Matches one `&nbsp;` that has no `&nbsp;` next to it. A run of 2 or more stays: it holds real double spaces. */
const SINGLE_NBSP = /(?<!&nbsp;)&nbsp;(?!&nbsp;)/g;

/**
 * Turns each single `&nbsp;` into a normal space. Quill 2 (`getSemanticHTML`, used by the PrimeNG editor) writes every
 * space as `&nbsp;`, and it reads `&nbsp;` back as a normal space on load. Without this step, stored text holds
 * `&nbsp;` for every space, and the same text written with normal spaces (e.g., seed data) counts as changed.
 */
export function normalizeRichTextHtml(html: string): string {
  return html.replace(SINGLE_NBSP, ' ');
}
