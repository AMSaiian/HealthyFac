import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

import { draftConflictKey } from './draft-conflict';

function failure(status: number, detail: unknown): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: { detail } });
}

describe('draftConflictKey', () => {
  it('draftConflictKey_With422DraftNotPending_ReturnsItsKey', () => {
    expect(draftConflictKey(failure(HttpStatusCode.UnprocessableEntity, 'draftNotPending'))).toBe(
      'DRAFTS.CONFLICTS.DRAFT_NOT_PENDING',
    );
  });

  it('draftConflictKey_With409ChangedTextOfCore_ReturnsDraftChanged', () => {
    const changed = 'The draft or the machine changed since you read it. Reload it and try again.';

    expect(draftConflictKey(failure(HttpStatusCode.Conflict, changed))).toBe('DRAFTS.CONFLICTS.DRAFT_CHANGED');
  });

  it('draftConflictKey_With409ChangedTextOfTelemetry_ReturnsDraftChanged', () => {
    const changed = 'The draft changed since you read it. Reload it and try again.';

    expect(draftConflictKey(failure(HttpStatusCode.Conflict, changed))).toBe('DRAFTS.CONFLICTS.DRAFT_CHANGED');
  });

  it('draftConflictKey_WithAReasonOfTheOtherStatus_ReturnsNull', () => {
    expect(draftConflictKey(failure(HttpStatusCode.Conflict, 'draftNotPending'))).toBeNull();
    expect(
      draftConflictKey(
        failure(HttpStatusCode.UnprocessableEntity, 'The draft changed since you read it. Reload it and try again.'),
      ),
    ).toBeNull();
  });

  it('draftConflictKey_WithOtherReasonOrError_ReturnsNull', () => {
    expect(draftConflictKey(failure(HttpStatusCode.UnprocessableEntity, 'another reason'))).toBeNull();
    expect(draftConflictKey(failure(HttpStatusCode.Conflict, 'A machine with this serial number exists.'))).toBeNull();
    expect(draftConflictKey(new HttpErrorResponse({ status: HttpStatusCode.NotFound }))).toBeNull();
    expect(draftConflictKey(new Error('x'))).toBeNull();
  });
});
