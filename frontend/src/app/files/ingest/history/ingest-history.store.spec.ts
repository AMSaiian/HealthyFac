import { ApplicationRef } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ActionStore } from '@shared/actions/action.store';

import { FileActionType } from '@files/file-actions';
import { IngestApi } from '@files/ingest/ingest-api';

import { IngestHistoryStore } from './ingest-history.store';

const URL = '/api/insights/v1/files/file-id/ingest-attempts';

describe('IngestHistoryStore', () => {
  function setup(): {
    store: InstanceType<typeof IngestHistoryStore>;
    http: HttpTestingController;
    actions: { dispatch: ReturnType<typeof vi.fn>; cancel: ReturnType<typeof vi.fn> };
  } {
    const actions = { dispatch: vi.fn(() => Promise.resolve()), cancel: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        IngestHistoryStore,
        IngestApi,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActionStore, useValue: actions },
      ],
    });
    const store = TestBed.inject(IngestHistoryStore);
    store.start({ type: FileActionType.IngestHistory, payload: { fileId: 'file-id' } });
    return { store, http: TestBed.inject(HttpTestingController), actions };
  }

  it('rows_WithSeveralAttempts_ListsTheNewestFirst', async () => {
    const { store, http } = setup();
    TestBed.tick();

    http.expectOne(URL).flush({
      items: [
        { id: 'old', queuedAt: '2026-03-01T08:00:00Z' },
        { id: 'new', queuedAt: '2026-03-02T08:00:00Z' },
      ],
    });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(store.rows().map((attempt) => attempt.id)).toEqual(['new', 'old']);
  });

  it('openSummary_WithAnAttempt_DispatchesTheSummaryAction', () => {
    const { store, actions } = setup();

    store.openSummary('attempt-id');

    expect(actions.dispatch).toHaveBeenCalledWith({
      type: FileActionType.IngestSummary,
      payload: { attemptId: 'attempt-id' },
    });
  });
});
