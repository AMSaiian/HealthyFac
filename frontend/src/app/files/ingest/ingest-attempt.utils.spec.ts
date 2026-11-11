import { type IngestAttempt, IngestErrorCode, IngestInterruptReason, IngestState } from '@files/ingest/ingest-attempt';

import { attemptReasonKey, parseWarning, toKeySegment } from './ingest-attempt.utils';

const ATTEMPT: IngestAttempt = {
  id: 'a1',
  fileId: 'f1',
  machineId: null,
  fileName: 'manual',
  extension: 'pdf',
  sizeBytes: 10,
  state: IngestState.Completed,
  errorCode: null,
  interruptReason: null,
  warning: null,
  queuedAt: '2026-03-01T08:00:00Z',
  startedAt: null,
  finishedAt: null,
  pageCount: null,
  chunkCount: null,
  proposedDraftCount: null,
};

describe('ingest attempt utils', () => {
  it('toKeySegment_WithCamelCase_ReturnsCapitalizedSnakeCase', () => {
    expect(toKeySegment('leaseExpired')).toBe('LEASE_EXPIRED');
    expect(toKeySegment('noText')).toBe('NO_TEXT');
  });

  it('attemptReasonKey_ForFailedAndInterrupted_ReturnsTheirKeys', () => {
    const failed = { ...ATTEMPT, state: IngestState.Failed, errorCode: IngestErrorCode.ProviderUnavailable };
    const interrupted = {
      ...ATTEMPT,
      state: IngestState.Interrupted,
      interruptReason: IngestInterruptReason.LeaseExpired,
    };

    expect(attemptReasonKey(failed)).toBe('INGEST.ERRORS.PROVIDER_UNAVAILABLE');
    expect(attemptReasonKey(interrupted)).toBe('INGEST.INTERRUPTED.LEASE_EXPIRED');
    expect(attemptReasonKey(ATTEMPT)).toBeNull();
  });

  it('parseWarning_WithKeyAndValues_ReadsBoth', () => {
    const warning = JSON.stringify({ key: 'INGEST.WARNINGS.TRUNCATED', values: { found: 90, cap: 50 } });

    expect(parseWarning(warning)).toEqual({ key: 'INGEST.WARNINGS.TRUNCATED', values: { found: 90, cap: 50 } });
  });

  it('parseWarning_WithPlainTextOrNull_ReturnsNull', () => {
    expect(parseWarning('not json')).toBeNull();
    expect(parseWarning(null)).toBeNull();
    expect(parseWarning('{"values":{}}')).toBeNull();
  });
});
