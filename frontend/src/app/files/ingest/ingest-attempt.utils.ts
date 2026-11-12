import { type IngestAttempt, IngestErrorCode, IngestInterruptReason, IngestState } from '@files/ingest/ingest-attempt';

const UPPER_SNAKE_BOUNDARY = /([a-z0-9])([A-Z])/g;

/** The translation key of each error code. Literal keys, so the translation check finds each one. */
const INGEST_ERROR_KEYS: Readonly<Record<IngestErrorCode, string>> = {
  [IngestErrorCode.FileNotFound]: 'INGEST.ERRORS.FILE_NOT_FOUND',
  [IngestErrorCode.NoText]: 'INGEST.ERRORS.NO_TEXT',
  [IngestErrorCode.TooManyPages]: 'INGEST.ERRORS.TOO_MANY_PAGES',
  [IngestErrorCode.ConversionFailed]: 'INGEST.ERRORS.CONVERSION_FAILED',
  [IngestErrorCode.ConversionTimeout]: 'INGEST.ERRORS.CONVERSION_TIMEOUT',
  [IngestErrorCode.InvalidExtraction]: 'INGEST.ERRORS.INVALID_EXTRACTION',
  [IngestErrorCode.ProviderUnavailable]: 'INGEST.ERRORS.PROVIDER_UNAVAILABLE',
};

/** The translation key of each reason of an interrupted attempt. Literal keys, so the translation check finds each one. */
const INGEST_INTERRUPT_KEYS: Readonly<Record<IngestInterruptReason, string>> = {
  [IngestInterruptReason.Cancelled]: 'INGEST.INTERRUPTED.CANCELLED',
  [IngestInterruptReason.LeaseExpired]: 'INGEST.INTERRUPTED.LEASE_EXPIRED',
  [IngestInterruptReason.ServiceStopped]: 'INGEST.INTERRUPTED.SERVICE_STOPPED',
  [IngestInterruptReason.FileDeleted]: 'INGEST.INTERRUPTED.FILE_DELETED',
};

/** Turns a camelCase value of the API into the capitalized snake case of a translation key. */
export function toKeySegment(value: string): string {
  return value.replace(UPPER_SNAKE_BOUNDARY, '$1_$2').toUpperCase();
}

export function ingestErrorKey(code: IngestErrorCode): string {
  return INGEST_ERROR_KEYS[code];
}

/** The translation key of the reason that a `failed` or `interrupted` attempt shows. Other states have none. */
export function attemptReasonKey(attempt: IngestAttempt): string | null {
  if (attempt.state === IngestState.Failed && attempt.errorCode !== null) {
    return ingestErrorKey(attempt.errorCode);
  }
  if (attempt.state === IngestState.Interrupted && attempt.interruptReason !== null) {
    return INGEST_INTERRUPT_KEYS[attempt.interruptReason];
  }
  return null;
}

export interface AttemptWarning {
  readonly key: string;
  readonly values: Readonly<Record<string, string | number>>;
}

/** Reads the warning of an attempt: a JSON text with a translation key and its values. Another text gives `null`. */
export function parseWarning(warning: string | null): AttemptWarning | null {
  if (warning === null) {
    return null;
  }
  try {
    const parsed = JSON.parse(warning) as { key?: unknown; values?: unknown };
    if (typeof parsed.key !== 'string') {
      return null;
    }
    const values = typeof parsed.values === 'object' && parsed.values !== null ? parsed.values : {};
    return { key: parsed.key, values: values as Record<string, string | number> };
  } catch {
    return null;
  }
}
