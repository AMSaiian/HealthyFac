import { array, custom, type infer as Infer, length, object, enum as zodEnum } from 'zod/mini';

import { Temporal } from '@shared/time/temporal';

import { Weekday } from '@employees/shifts/shift';
import { shiftBlockListSchema } from '@employees/shifts/shift-blocks-schema';
import { WEEKDAY_COUNT } from '@employees/shifts/shift-limits';

export const DATE_REQUIRED_ERROR_KEY = 'SHIFTS.ERRORS.DATE_REQUIRED';
export const DAYS_ERROR_KEY = 'SHIFTS.ERRORS.DAYS';

/** The version form: the first day of effect and exactly one entry (with its blocks) for each weekday. */
export const versionFormSchema = object({
  effectiveFrom: custom<Temporal.PlainDate | null>(
    (value) => value instanceof Temporal.PlainDate,
    DATE_REQUIRED_ERROR_KEY,
  ),
  days: array(object({ weekday: zodEnum(Weekday), blocks: shiftBlockListSchema })).check(
    length(WEEKDAY_COUNT, DAYS_ERROR_KEY),
  ),
});

export type VersionFormValue = Infer<typeof versionFormSchema>;
