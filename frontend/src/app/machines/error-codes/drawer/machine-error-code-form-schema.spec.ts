import { HTML_MAX_LENGTH } from '@shared/rich-text/rich-text-limits';
import { TOO_MUCH_FORMATTING_ERROR_KEY } from '@shared/rich-text/rich-text-schema';

import { CODE_MAX_LENGTH, DESCRIPTION_MAX_LENGTH } from '@machines/error-codes/machine-error-code-limits';

import { machineErrorCodeFormSchema, type MachineErrorCodeFormValue } from './machine-error-code-form-schema';

const VALID: MachineErrorCodeFormValue = { code: 'E-101', description: '<p>text</p>' };

describe('machineErrorCodeFormSchema', () => {
  it('machineErrorCodeFormSchema_WithValidValue_Passes', () => {
    const result = machineErrorCodeFormSchema.safeParse(VALID);

    expect(result.success).toBe(true);
  });

  it('machineErrorCodeFormSchema_WithBlankCode_Fails', () => {
    const result = machineErrorCodeFormSchema.safeParse({ ...VALID, code: '   ' });

    expect(result.success).toBe(false);
  });

  it('machineErrorCodeFormSchema_WithCodeAboveLimit_Fails', () => {
    const result = machineErrorCodeFormSchema.safeParse({ ...VALID, code: 'a'.repeat(CODE_MAX_LENGTH + 1) });

    expect(result.success).toBe(false);
  });

  it('machineErrorCodeFormSchema_WithCodeAtLimit_Passes', () => {
    const result = machineErrorCodeFormSchema.safeParse({ ...VALID, code: 'a'.repeat(CODE_MAX_LENGTH) });

    expect(result.success).toBe(true);
  });

  it('machineErrorCodeFormSchema_WithDescriptionAboveLimit_Fails', () => {
    const result = machineErrorCodeFormSchema.safeParse({
      ...VALID,
      description: 'a'.repeat(DESCRIPTION_MAX_LENGTH + 1),
    });

    expect(result.success).toBe(false);
  });

  it('machineErrorCodeFormSchema_WithDescriptionAtLimitOrNull_Passes', () => {
    const atLimit = machineErrorCodeFormSchema.safeParse({
      ...VALID,
      description: 'a'.repeat(DESCRIPTION_MAX_LENGTH),
    });
    const empty = machineErrorCodeFormSchema.safeParse({ ...VALID, description: null });

    expect([atLimit.success, empty.success]).toEqual([true, true]);
  });

  it('machineErrorCodeFormSchema_WithVisibleTextAtLimitInsideFormatting_Passes', () => {
    const description = `<p>${'<strong>aaaaaaaaaa</strong>'.repeat(DESCRIPTION_MAX_LENGTH / 10)}</p>`;

    const result = machineErrorCodeFormSchema.safeParse({ ...VALID, description });

    expect(description.length).toBeGreaterThan(DESCRIPTION_MAX_LENGTH);
    expect(result.success).toBe(true);
  });

  it('machineErrorCodeFormSchema_WithHtmlAboveCap_FailsWithFormattingKey', () => {
    const result = machineErrorCodeFormSchema.safeParse({ ...VALID, description: 'a'.repeat(HTML_MAX_LENGTH + 1) });

    expect(result.error?.issues.map((issue) => issue.message)).toContain(TOO_MUCH_FORMATTING_ERROR_KEY);
  });
});
