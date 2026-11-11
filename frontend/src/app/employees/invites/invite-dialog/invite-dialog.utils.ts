import { safeParse } from 'zod/mini';

import { NAME_MAX_LENGTH } from '@shared/employee/person-name-limits';

import { inviteFormSchema, type InviteFormValue } from './invite-dialog-schema';

export const INVALID_REASON_KEY = 'EMPLOYEES.INVITE.INVALID';
export const NAME_TOO_LONG_REASON_KEY = 'EMPLOYEES.INVITE.NAME_TOO_LONG';

export interface SaveReason {
  readonly key: string;
  readonly params?: { readonly max: number };
}

/**
 * Gives the first real reason why **Save** is disabled, or `null` when the form is valid. An email that is not valid
 * comes first, then a name over the limit.
 */
export function inviteSaveReason(value: InviteFormValue | null, isValid: boolean): SaveReason | null {
  if (isValid) {
    return null;
  }
  if (value === null || !safeParse(inviteFormSchema.shape.email, value.email).success) {
    return { key: INVALID_REASON_KEY };
  }
  if (value.firstName.trim().length > NAME_MAX_LENGTH || value.lastName.trim().length > NAME_MAX_LENGTH) {
    return { key: NAME_TOO_LONG_REASON_KEY, params: { max: NAME_MAX_LENGTH } };
  }
  return { key: INVALID_REASON_KEY };
}
