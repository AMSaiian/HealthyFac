import { type infer as Infer, maxLength, minLength, object, regex, string, trim, enum as zodEnum } from 'zod/mini';

import { EmployeeRole } from '@shared/employee/employee';
import { NAME_MAX_LENGTH } from '@shared/employee/person-name-limits';

export const EMAIL_MAX_LENGTH = 255;
export const EMAIL_REQUIRED_ERROR_KEY = 'EMPLOYEES.INVITE.ERRORS.EMAIL_REQUIRED';
export const EMAIL_INVALID_ERROR_KEY = 'EMPLOYEES.INVITE.ERRORS.EMAIL_INVALID';
export const EMAIL_TOO_LONG_ERROR_KEY = 'EMPLOYEES.INVITE.ERRORS.EMAIL_TOO_LONG';
export const NAME_TOO_LONG_ERROR_KEY = 'EMPLOYEES.INVITE.ERRORS.NAME_TOO_LONG';

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

const optionalName = (): ReturnType<typeof string> =>
  string().check(trim(), maxLength(NAME_MAX_LENGTH, NAME_TOO_LONG_ERROR_KEY));

/** The invite form: the email, the role, and the optional names. The messages are translation keys. */
export const inviteFormSchema = object({
  email: string().check(
    trim(),
    minLength(1, EMAIL_REQUIRED_ERROR_KEY),
    maxLength(EMAIL_MAX_LENGTH, EMAIL_TOO_LONG_ERROR_KEY),
    regex(EMAIL_PATTERN, EMAIL_INVALID_ERROR_KEY),
  ),
  roleCode: zodEnum(EmployeeRole),
  firstName: optionalName(),
  lastName: optionalName(),
});

export type InviteFormValue = Infer<typeof inviteFormSchema>;
