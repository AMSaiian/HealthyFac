import { custom, type infer as Infer, minLength, object, string } from 'zod/mini';

import { Temporal } from '@shared/time/temporal';

export const ASSIGNEE_REQUIRED_ERROR_KEY = 'WORK_ORDERS.SCHEDULE.ERRORS.ASSIGNEE_REQUIRED';
export const START_REQUIRED_ERROR_KEY = 'WORK_ORDERS.SCHEDULE.ERRORS.START_REQUIRED';

/**
 * The assign and move form: the person and the planned start as a local date and time of the company zone. The start
 * is `null` until the user or the suggestion sets it, and `null` is not valid.
 */
export const assignFormSchema = object({
  assigneeId: string().check(minLength(1, ASSIGNEE_REQUIRED_ERROR_KEY)),
  start: custom<Temporal.PlainDateTime | null>(
    (value) => value instanceof Temporal.PlainDateTime,
    START_REQUIRED_ERROR_KEY,
  ),
});

export type AssignFormValue = Infer<typeof assignFormSchema>;
