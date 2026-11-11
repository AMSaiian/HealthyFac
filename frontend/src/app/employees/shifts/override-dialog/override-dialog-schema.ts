import { custom, type infer as Infer, object } from 'zod/mini';

import { Temporal } from '@shared/time/temporal';

import { shiftBlockListSchema } from '@employees/shifts/shift-blocks-schema';
import { DATE_REQUIRED_ERROR_KEY } from '@employees/shifts/version-dialog/version-dialog-schema';

/** The override form: one date and its blocks. No block means a day off. */
export const overrideFormSchema = object({
  date: custom<Temporal.PlainDate | null>((value) => value instanceof Temporal.PlainDate, DATE_REQUIRED_ERROR_KEY),
  blocks: shiftBlockListSchema,
});

export type OverrideFormValue = Infer<typeof overrideFormSchema>;
