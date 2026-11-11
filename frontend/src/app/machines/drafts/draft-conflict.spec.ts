import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

import { draftConflictKey } from './draft-conflict';

function conflict(detail: unknown): HttpErrorResponse {
  return new HttpErrorResponse({ status: HttpStatusCode.Conflict, error: { detail } });
}

describe('draftConflictKey', () => {
  it('draftConflictKey_WithKnownReason_ReturnsItsKey', () => {
    expect(draftConflictKey(conflict('draftNotPending'))).toBe('DRAFTS.CONFLICTS.DRAFT_NOT_PENDING');
    expect(draftConflictKey(conflict('machineDraftNotApplied'))).toBe('DRAFTS.CONFLICTS.MACHINE_DRAFT_NOT_APPLIED');
  });

  it('draftConflictKey_WithOtherConflictOrError_ReturnsNull', () => {
    expect(draftConflictKey(conflict('A machine with this serial number exists.'))).toBeNull();
    expect(draftConflictKey(new HttpErrorResponse({ status: HttpStatusCode.NotFound }))).toBeNull();
    expect(draftConflictKey(new Error('x'))).toBeNull();
  });
});
