import { HTML_MAX_LENGTH } from '@shared/rich-text/rich-text-limits';
import { TOO_MUCH_FORMATTING_ERROR_KEY } from '@shared/rich-text/rich-text-schema';

import { DESCRIPTION_MAX_LENGTH } from '@machines/machine-limits';

import { machineFormSchema, type MachineFormValue } from './machine-form-schema';

const VALID: MachineFormValue = {
  name: 'Lathe 01',
  serialNumber: 'ABC-1',
  departmentId: 'department-1',
  description: '<p>text</p>',
};

describe('machineFormSchema description', () => {
  const formatted = (visibleLength: number): string =>
    `<p>${'<strong>aaaaaaaaaa</strong>'.repeat(visibleLength / 10)}</p>`;

  it('machineFormSchema_WithVisibleTextAtLimitInsideFormatting_Passes', () => {
    const description = formatted(DESCRIPTION_MAX_LENGTH);

    const result = machineFormSchema.safeParse({ ...VALID, description });

    expect(description.length).toBeGreaterThan(DESCRIPTION_MAX_LENGTH);
    expect(result.success).toBe(true);
  });

  it('machineFormSchema_WithVisibleTextAboveLimit_Fails', () => {
    const result = machineFormSchema.safeParse({
      ...VALID,
      description: `<p>${'a'.repeat(DESCRIPTION_MAX_LENGTH + 1)}</p>`,
    });

    expect(result.success).toBe(false);
  });

  it('machineFormSchema_WithHtmlAboveCap_FailsWithFormattingKey', () => {
    const result = machineFormSchema.safeParse({ ...VALID, description: 'a'.repeat(HTML_MAX_LENGTH + 1) });

    expect(result.error?.issues.map((issue) => issue.message)).toContain(TOO_MUCH_FORMATTING_ERROR_KEY);
  });
});
