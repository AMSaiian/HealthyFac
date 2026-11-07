import { HTML_MAX_LENGTH } from '@shared/rich-text/rich-text-limits';
import { TOO_MUCH_FORMATTING_ERROR_KEY } from '@shared/rich-text/rich-text-schema';

import { DESCRIPTION_MAX_LENGTH } from '@machines/statuses/status-definition-limits';

import { StatusSeverity } from '@machines/statuses/status-definition';

import { statusDefinitionFormSchema, type StatusDefinitionFormValue } from './status-definition-form-schema';

const VALID: StatusDefinitionFormValue = { code: 'E-101', severity: StatusSeverity.Info, description: '<p>text</p>' };

describe('statusDefinitionFormSchema', () => {
  it('statusDefinitionFormSchema_WithVisibleTextAtLimitInsideFormatting_Passes', () => {
    const description = `<p>${'<strong>aaaaaaaaaa</strong>'.repeat(DESCRIPTION_MAX_LENGTH / 10)}</p>`;

    const result = statusDefinitionFormSchema.safeParse({ ...VALID, description });

    expect(description.length).toBeGreaterThan(DESCRIPTION_MAX_LENGTH);
    expect(result.success).toBe(true);
  });

  it('statusDefinitionFormSchema_WithHtmlAboveCap_FailsWithFormattingKey', () => {
    const result = statusDefinitionFormSchema.safeParse({ ...VALID, description: 'a'.repeat(HTML_MAX_LENGTH + 1) });

    expect(result.error?.issues.map((issue) => issue.message)).toContain(TOO_MUCH_FORMATTING_ERROR_KEY);
  });
});
