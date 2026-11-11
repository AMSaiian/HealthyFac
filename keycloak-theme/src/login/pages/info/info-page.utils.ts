const TAG = /<[^>]*>/g;
const WHITESPACE = /\s+/g;
const REQUIRED_ACTION_PREFIX = 'requiredAction.';

function plainText(html: string): string {
  return html.replace(TAG, ' ').replace(WHITESPACE, ' ').trim().toLowerCase();
}

/** Gives the summary only when it says something other than the title, so the page never repeats the title. */
export function summaryToShow(title: string, summary: string): string | null {
  return plainText(title) === plainText(summary) ? null : summary;
}

/** The message keys of the required actions, in the order that Keycloak gives them. */
export function requiredActionKeys(actions: readonly string[] | undefined): string[] {
  return (actions ?? []).map((action) => `${REQUIRED_ACTION_PREFIX}${action}`);
}

/**
 * The title of the info page: the translated header key, or the summary when there is no header or the translation
 * gives the key back (the bundle has no text for it).
 */
export function titleOf(headerKey: string | undefined, summary: string, translate: (key: string) => string): string {
  if (headerKey === undefined) {
    return translate(summary);
  }
  const translated = translate(headerKey);
  return translated === headerKey || translated === '' ? translate(summary) : translated;
}
