import { HTML_MAX_LENGTH } from '@shared/rich-text/rich-text-limits';
import { TOO_MUCH_FORMATTING_ERROR_KEY } from '@shared/rich-text/rich-text-schema';

import { DESCRIPTION_MAX_LENGTH, NAME_MAX_LENGTH, SERIAL_NUMBER_MAX_LENGTH } from '@machines/machine-limits';

import { machineActionPayloadSchema, machineFormSchema, type MachineFormValue } from './machine-form-schema';

const VALID: MachineFormValue = {
  name: 'Lathe 01',
  serialNumber: 'ABC-1',
  departmentId: 'department-1',
  description: '<p>text</p>',
};

describe('machineFormSchema', () => {
  it('machineFormSchema_WithBlankName_Fails', () => {
    const result = machineFormSchema.safeParse({ ...VALID, name: '   ' });

    expect(result.success).toBe(false);
  });

  it('machineFormSchema_WithValidValue_Passes', () => {
    const result = machineFormSchema.safeParse(VALID);

    expect(result.success).toBe(true);
  });

  it('machineFormSchema_WithDescriptionAboveLimit_Fails', () => {
    const result = machineFormSchema.safeParse({ ...VALID, description: 'a'.repeat(DESCRIPTION_MAX_LENGTH + 1) });

    expect(result.success).toBe(false);
  });

  it('machineFormSchema_WithDescriptionAtLimitOrNull_Passes', () => {
    const atLimit = machineFormSchema.safeParse({ ...VALID, description: 'a'.repeat(DESCRIPTION_MAX_LENGTH) });
    const empty = machineFormSchema.safeParse({ ...VALID, description: null });

    expect([atLimit.success, empty.success]).toEqual([true, true]);
  });

  it('machineFormSchema_WithTooLongNameOrSerialNumber_Fails', () => {
    const name = machineFormSchema.safeParse({ ...VALID, name: 'a'.repeat(NAME_MAX_LENGTH + 1) });
    const serial = machineFormSchema.safeParse({
      ...VALID,
      serialNumber: 'a'.repeat(SERIAL_NUMBER_MAX_LENGTH + 1),
    });

    expect([name.success, serial.success]).toEqual([false, false]);
  });

  it('machineFormSchema_WithoutDepartment_Fails', () => {
    const result = machineFormSchema.safeParse({ ...VALID, departmentId: '' });

    expect(result.success).toBe(false);
  });
});

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

describe('machineActionPayloadSchema', () => {
  it('machineActionPayloadSchema_WithPartialValues_Passes', () => {
    const result = machineActionPayloadSchema.safeParse({ name: 'Lathe 02', machineId: 'encrypted-id' });

    expect(result.success).toBe(true);
  });

  it('machineActionPayloadSchema_WithInvalidValue_Fails', () => {
    const result = machineActionPayloadSchema.safeParse({ name: '' });

    expect(result.success).toBe(false);
  });
});
