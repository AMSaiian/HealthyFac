import {
  extend,
  type infer as Infer,
  maxLength,
  minLength,
  nullable,
  object,
  optional,
  partial,
  string,
  trim,
  type ZodMiniString,
} from 'zod/mini';

import { DESCRIPTION_MAX_LENGTH, NAME_MAX_LENGTH, SERIAL_NUMBER_MAX_LENGTH } from '@machines/machine-limits';

export const REQUIRED_ERROR_KEY = 'MACHINES.FORM.ERRORS.REQUIRED';
export const TOO_LONG_ERROR_KEY = 'MACHINES.FORM.ERRORS.TOO_LONG';

const requiredText = (limit: number): ZodMiniString =>
  string().check(trim(), minLength(1, REQUIRED_ERROR_KEY), maxLength(limit, TOO_LONG_ERROR_KEY));

/**
 * The one schema of a machine. The drawer form validates with it, and the action dispatcher validates the payload of
 * an action with its partial form. The messages are translation keys.
 */
export const machineFormSchema = object({
  name: requiredText(NAME_MAX_LENGTH),
  serialNumber: requiredText(SERIAL_NUMBER_MAX_LENGTH),
  departmentId: string().check(minLength(1, REQUIRED_ERROR_KEY)),
  description: nullable(string().check(maxLength(DESCRIPTION_MAX_LENGTH, TOO_LONG_ERROR_KEY))),
});

export type MachineFormValue = Infer<typeof machineFormSchema>;

export const machineActionPayloadSchema = extend(partial(machineFormSchema), {
  machineId: optional(string()),
});

export type MachineActionPayload = Infer<typeof machineActionPayloadSchema>;
