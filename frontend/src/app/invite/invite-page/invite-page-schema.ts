import { type infer as Infer, maxLength, minLength, object, string, trim } from 'zod/mini';

import { NAME_MAX_LENGTH } from '@shared/employee/person-name-limits';

export const NAME_REQUIRED_ERROR_KEY = 'FORMS.REQUIRED';
export const NAME_TOO_LONG_ERROR_KEY = 'INVITE.ERRORS.NAME_TOO_LONG';

const nameText = (): ReturnType<typeof string> =>
  string().check(trim(), minLength(1, NAME_REQUIRED_ERROR_KEY), maxLength(NAME_MAX_LENGTH, NAME_TOO_LONG_ERROR_KEY));

/** The accept form: the first and the last name of the new employee. The limit mirrors the backend. */
export const acceptFormSchema = object({ firstName: nameText(), lastName: nameText() });

export type AcceptFormValue = Infer<typeof acceptFormSchema>;
