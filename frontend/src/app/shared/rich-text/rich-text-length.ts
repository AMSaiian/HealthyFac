const HTML_MIME_TYPE = 'text/html';

/**
 * Counts the visible characters of an HTML string, the same way as the backend: parse the HTML, take the text content
 * of the body, apply Unicode normalization form C, and take the UTF-16 length. Tags, line breaks, and empty markup
 * count 0, and an entity counts as its character.
 */
export function visibleTextLength(html: string | null): number {
  if (html === null || html === '') {
    return 0;
  }
  const textContent = new DOMParser().parseFromString(html, HTML_MIME_TYPE).body.textContent ?? '';
  return textContent.normalize('NFC').length;
}
