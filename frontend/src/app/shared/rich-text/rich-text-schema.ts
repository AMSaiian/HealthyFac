import { maxLength, nullable, overwrite, refine, string, type ZodMiniNullable, type ZodMiniString } from 'zod/mini';

import { normalizeRichTextHtml } from '@shared/rich-text/rich-text-html';
import { HTML_MAX_LENGTH } from '@shared/rich-text/rich-text-limits';
import { visibleTextLength } from '@shared/rich-text/rich-text-length';

export const TOO_MUCH_FORMATTING_ERROR_KEY = 'RICH_TEXT.ERRORS.TOO_MUCH_FORMATTING';

/**
 * The schema of a rich text field: the raw HTML stays under the safety cap, and the visible text stays under the
 * limit of the feature. The messages are translation keys.
 */
export function richTextSchema(maxVisibleLength: number, tooLongKey: string): ZodMiniNullable<ZodMiniString> {
  return nullable(
    string().check(
      overwrite(normalizeRichTextHtml),
      maxLength(HTML_MAX_LENGTH, { error: TOO_MUCH_FORMATTING_ERROR_KEY, abort: true }),
      refine((html) => visibleTextLength(html) <= maxVisibleLength, tooLongKey),
    ),
  );
}
