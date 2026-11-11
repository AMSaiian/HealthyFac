import { EmployeeRole } from '@shared/employee/employee';

import type { InviteFormValue } from './invite-dialog-schema';
import { INVALID_REASON_KEY, inviteSaveReason, NAME_TOO_LONG_REASON_KEY } from './invite-dialog.utils';

const VALID: InviteFormValue = {
  email: 'new@healthyfac.test',
  roleCode: EmployeeRole.Worker,
  firstName: '',
  lastName: '',
};

describe('inviteSaveReason', () => {
  it('inviteSaveReason_WithAValidForm_GivesNoReason', () => {
    expect(inviteSaveReason(VALID, true)).toBeNull();
  });

  it('inviteSaveReason_WithAnInvalidEmail_AsksForAValidEmail', () => {
    const reason = inviteSaveReason({ ...VALID, email: 'not-an-email', firstName: 'x'.repeat(101) }, false);

    expect(reason).toEqual({ key: INVALID_REASON_KEY });
  });

  it('inviteSaveReason_WithAValidEmailAndALongFirstName_NamesTheLimit', () => {
    const reason = inviteSaveReason({ ...VALID, firstName: 'x'.repeat(101) }, false);

    expect(reason).toEqual({ key: NAME_TOO_LONG_REASON_KEY, params: { max: 100 } });
  });

  it('inviteSaveReason_WithAValidEmailAndALongLastName_NamesTheLimit', () => {
    const reason = inviteSaveReason({ ...VALID, lastName: 'x'.repeat(101) }, false);

    expect(reason?.key).toBe(NAME_TOO_LONG_REASON_KEY);
  });

  it('inviteSaveReason_WithNoValueYet_AsksForAValidEmail', () => {
    expect(inviteSaveReason(null, false)).toEqual({ key: INVALID_REASON_KEY });
  });
});
