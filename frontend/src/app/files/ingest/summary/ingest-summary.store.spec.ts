import { ApplicationRef } from '@angular/core';
import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { FileActionType } from '@files/file-actions';
import { IngestApi } from '@files/ingest/ingest-api';
import { IngestState } from '@files/ingest/ingest-attempt';

import { IngestSummaryStore } from './ingest-summary.store';

const ATTEMPT_URL = '/api/insights/v1/ingest-attempts/attempt-id';

const ATTEMPT = {
  id: 'attempt-id',
  fileId: 'file-id',
  machineId: null,
  fileName: 'manual',
  extension: 'pdf',
  sizeBytes: 10,
  state: IngestState.Failed,
  errorCode: 'noText',
  interruptReason: null,
  warning: null,
  queuedAt: '2026-03-01T08:00:00Z',
  startedAt: null,
  finishedAt: null,
  pageCount: null,
  chunkCount: null,
  proposedDraftCount: null,
};

describe('IngestSummaryStore', () => {
  function setup(): { store: InstanceType<typeof IngestSummaryStore>; http: HttpTestingController } {
    TestBed.configureTestingModule({
      providers: [IngestSummaryStore, IngestApi, provideHttpClient(), provideHttpClientTesting()],
    });
    const store = TestBed.inject(IngestSummaryStore);
    store.start({ type: FileActionType.IngestSummary, payload: { attemptId: 'attempt-id' } });
    return { store, http: TestBed.inject(HttpTestingController) };
  }

  async function settle(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
  }

  it('load_WithFailedAttempt_ShowsTheReasonOfTheFailure', async () => {
    const { store, http } = setup();
    TestBed.tick();

    http.expectOne(ATTEMPT_URL).flush(ATTEMPT);
    await settle();

    expect(store.reasonKey()).toBe('INGEST.ERRORS.NO_TEXT');
    expect(store.isNotFound()).toBe(false);
  });

  it('load_WithUnknownAttempt_ShowsNotFound', async () => {
    const { store, http } = setup();
    TestBed.tick();

    http.expectOne(ATTEMPT_URL).flush(null, { status: HttpStatusCode.NotFound, statusText: 'Not Found' });
    await settle();

    expect(store.isNotFound()).toBe(true);
  });
});
