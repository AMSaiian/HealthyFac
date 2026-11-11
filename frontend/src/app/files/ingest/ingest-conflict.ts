import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

const CONFLICT_KEYS: ReadonlyMap<string, string> = new Map([
  ['ingestActive', 'INGEST.CONFLICTS.INGEST_ACTIVE'],
  ['ingestCompleted', 'INGEST.CONFLICTS.INGEST_COMPLETED'],
  ['ingestFailedLimit', 'INGEST.CONFLICTS.INGEST_FAILED_LIMIT'],
  ['ingestFinal', 'INGEST.CONFLICTS.INGEST_FINAL'],
]);

const FALLBACK_KEY = 'ERRORS.HTTP.CONFLICT';

/** The translation key of a `409` of the ingest API. The `detail` of the problem holds the reason code. */
export function ingestConflictKey(error: unknown): string | null {
  if (!(error instanceof HttpErrorResponse) || error.status !== HttpStatusCode.Conflict) {
    return null;
  }
  const detail = (error.error as { detail?: unknown } | null)?.detail;
  return (typeof detail === 'string' ? CONFLICT_KEYS.get(detail) : undefined) ?? FALLBACK_KEY;
}
