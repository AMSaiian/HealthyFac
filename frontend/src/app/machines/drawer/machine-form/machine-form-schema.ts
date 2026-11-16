import {
  custom,
  extend,
  type infer as Infer,
  maxLength,
  minLength,
  object,
  optional,
  partial,
  string,
  trim,
  type ZodMiniString,
} from 'zod/mini';

import { richTextSchema } from '@shared/rich-text/rich-text-schema';
import { Temporal } from '@shared/time/temporal';

import { MachineState } from '@machines/machine';
import { DESCRIPTION_MAX_LENGTH, NAME_MAX_LENGTH, SERIAL_NUMBER_MAX_LENGTH } from '@machines/machine-limits';

export const REQUIRED_ERROR_KEY = 'FORMS.REQUIRED';
export const TOO_LONG_ERROR_KEY = 'FORMS.TOO_LONG';

const requiredText = (limit: number): ZodMiniString =>
  string().check(trim(), minLength(1, REQUIRED_ERROR_KEY), maxLength(limit, TOO_LONG_ERROR_KEY));

/**
 * The text fields of a machine. The compare dialog merges only these fields, and the action dispatcher validates the
 * payload of an action with their partial form. A payload is JSON, so it holds no go-live date. The messages are
 * translation keys.
 */
export const machineFieldsSchema = object({
  name: requiredText(NAME_MAX_LENGTH),
  serialNumber: requiredText(SERIAL_NUMBER_MAX_LENGTH),
  description: richTextSchema(DESCRIPTION_MAX_LENGTH, TOO_LONG_ERROR_KEY),
});

export type MachineFieldsValue = Infer<typeof machineFieldsSchema>;

const MACHINE_STATES: readonly unknown[] = Object.values(MachineState);

/** The one schema of the drawer form: the text fields, the go-live date, and the state (`null` on create). */
export const machineFormSchema = extend(machineFieldsSchema, {
  expectedGoLiveOn: custom<Temporal.PlainDate | null>(
    (value) => value instanceof Temporal.PlainDate,
    REQUIRED_ERROR_KEY,
  ),
  state: custom<MachineState | null>((value) => value === null || MACHINE_STATES.includes(value), REQUIRED_ERROR_KEY),
});

export type MachineFormValue = Infer<typeof machineFormSchema>;

export const machineActionPayloadSchema = extend(partial(machineFieldsSchema), {
  machineId: optional(string()),
  /** The create draft that the drawer applies. The drawer then loads the draft values. */
  draftId: optional(string()),
});

export type MachineActionPayload = Infer<typeof machineActionPayloadSchema>;
