import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

/** The reason codes of the `409` of the draft API, and the text of the "changed" conflict, in the problem `detail`. */
const CONFLICT_KEYS: ReadonlyMap<string, string> = new Map([
  ['draftNotPending', 'DRAFTS.CONFLICTS.DRAFT_NOT_PENDING'],
  ['machineDraftNotApplied', 'DRAFTS.CONFLICTS.MACHINE_DRAFT_NOT_APPLIED'],
  ['The draft or the machine changed since you read it. Reload it and try again.', 'DRAFTS.CONFLICTS.DRAFT_CHANGED'],
]);

/** The translation key of a `409` of the draft API, or `null` for another error or another conflict. */
export function draftConflictKey(error: unknown): string | null {
  if (!(error instanceof HttpErrorResponse) || error.status !== HttpStatusCode.Conflict) {
    return null;
  }
  const detail = (error.error as { detail?: unknown } | null)?.detail;
  return typeof detail === 'string' ? (CONFLICT_KEYS.get(detail) ?? null) : null;
}
