import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

/**
 * The reason code of the `422` of the draft API, in the problem `detail`: a draft that is not pending, a phase draft
 * that still uses a draft, and a metric or status draft that an open phase draft uses.
 */
const UNPROCESSABLE_KEYS: ReadonlyMap<string, string> = new Map([
  ['draftNotPending', 'DRAFTS.CONFLICTS.DRAFT_NOT_PENDING'],
  ['draftUsesDrafts', 'DRAFTS.CONFLICTS.DRAFT_USES_DRAFTS'],
  ['draftUsedByPhaseDraft', 'DRAFTS.CONFLICTS.DRAFT_USED_BY_PHASE_DRAFT'],
]);

/** The texts of the "changed" conflict of the `409` of the draft API (Core, then Telemetry), in the problem `detail`. */
const CONFLICT_KEYS: ReadonlyMap<string, string> = new Map([
  ['The draft or the machine changed since you read it. Reload it and try again.', 'DRAFTS.CONFLICTS.DRAFT_CHANGED'],
  ['The draft changed since you read it. Reload it and try again.', 'DRAFTS.CONFLICTS.DRAFT_CHANGED'],
]);

const KEYS_BY_STATUS: ReadonlyMap<number, ReadonlyMap<string, string>> = new Map([
  [HttpStatusCode.UnprocessableEntity, UNPROCESSABLE_KEYS],
  [HttpStatusCode.Conflict, CONFLICT_KEYS],
]);

/**
 * The translation key of a `409` or `422` of the draft API, or `null` for another error or another reason. A `422`
 * reads only the reason codes of the `422`, and a `409` only those of the `409`.
 */
export function draftConflictKey(error: unknown): string | null {
  if (!(error instanceof HttpErrorResponse)) {
    return null;
  }
  const detail = (error.error as { detail?: unknown } | null)?.detail;
  return typeof detail === 'string' ? (KEYS_BY_STATUS.get(error.status)?.get(detail) ?? null) : null;
}
