import { type infer as Infer, maxLength, minLength, object, string, trim } from 'zod/mini';

import { richTextSchema } from '@shared/rich-text/rich-text-schema';

import { CODE_MAX_LENGTH, DESCRIPTION_MAX_LENGTH } from '@machines/error-codes/machine-error-code-limits';

export const REQUIRED_ERROR_KEY = 'MACHINE_ERROR_CODES.FORM.ERRORS.REQUIRED';
export const TOO_LONG_ERROR_KEY = 'MACHINE_ERROR_CODES.FORM.ERRORS.TOO_LONG';

/** The one schema of an error code. The dialog form validates with it. The messages are translation keys. */
export const machineErrorCodeFormSchema = object({
  code: string().check(trim(), minLength(1, REQUIRED_ERROR_KEY), maxLength(CODE_MAX_LENGTH, TOO_LONG_ERROR_KEY)),
  description: richTextSchema(DESCRIPTION_MAX_LENGTH, TOO_LONG_ERROR_KEY),
});

export type MachineErrorCodeFormValue = Infer<typeof machineErrorCodeFormSchema>;
