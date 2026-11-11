import { type IngestAttempt, type IngestErrorCode, IngestState } from '@files/ingest/ingest-attempt';

const UPPER_SNAKE_BOUNDARY = /([a-z0-9])([A-Z])/g;

/** Turns a camelCase value of the API into the capitalized snake case of a translation key. */
export function toKeySegment(value: string): string {
  return value.replace(UPPER_SNAKE_BOUNDARY, '$1_$2').toUpperCase();
}

export function ingestErrorKey(code: IngestErrorCode): string {
  return `INGEST.ERRORS.${toKeySegment(code)}`;
}

/** The translation key of the reason that a `failed` or `interrupted` attempt shows. Other states have none. */
export function attemptReasonKey(attempt: IngestAttempt): string | null {
  if (attempt.state === IngestState.Failed && attempt.errorCode !== null) {
    return ingestErrorKey(attempt.errorCode);
  }
  if (attempt.state === IngestState.Interrupted && attempt.interruptReason !== null) {
    return `INGEST.INTERRUPTED.${toKeySegment(attempt.interruptReason)}`;
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
