import { ApplicationRef } from '@angular/core';
import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';

import { ActionStore } from '@shared/actions/action.store';

import { DraftActionType } from '@machines/drafts/draft-actions';
import { DraftsApi } from '@machines/drafts/drafts-api';
import { StatusCompareField } from '@machines/drafts/status/status-draft-review.store.utils';
import { StatusDefinitionsApi } from '@machines/statuses/status-definitions-api';

import { StatusDraftReviewStore } from './status-draft-review.store';

const DRAFTS_URL = '/api/core/v1/ingest-attempts/attempt-1/drafts';
const SEARCH_URL = '/api/telemetry/v1/machines/machine-1/status-definitions/search';
const MARK_URL = '/api/core/v1/drafts/mark-applied';

const DRAFTS = {
  groups: [
    {
      machineId: 'machine-1',
      machineName: 'Press',
      machineDraft: null,
      metrics: [],
      statuses: [
        {
          id: 'draft-1',
          state: 'pending',
          sourceLocation: 'page 2',
          code: 'e12',
          severity: 'error',
          description: null,
        },
      ],
    },
  ],
};

const EXISTING = {
  id: 'status-1',
  code: 'E12',
  severity: 'warning',
  description: '<p>old</p>',
  createdAt: 'x',
  updatedAt: null,
};

describe('StatusDraftReviewStore', () => {
  function setup(): {
    store: InstanceType<typeof StatusDraftReviewStore>;
    http: HttpTestingController;
    actions: { complete: ReturnType<typeof vi.fn>; cancel: ReturnType<typeof vi.fn> };
    messages: { add: ReturnType<typeof vi.fn> };
  } {
    const actions = { complete: vi.fn(), cancel: vi.fn() };
    const messages = { add: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        StatusDraftReviewStore,
        DraftsApi,
        StatusDefinitionsApi,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActionStore, useValue: actions },
        { provide: MessageService, useValue: messages },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
      ],
    });
    const store = TestBed.inject(StatusDraftReviewStore);
    store.start({
      type: DraftActionType.ReviewStatus,
      payload: { machineId: 'machine-1', attemptId: 'attempt-1', draftId: 'draft-1' },
    });
    return { store, http: TestBed.inject(HttpTestingController), actions, messages };
  }

  async function load(http: HttpTestingController, existing: typeof EXISTING | null): Promise<void> {
    TestBed.tick();
    http.expectOne(DRAFTS_URL).flush(DRAFTS);
    await vi.waitFor(() => {
      TestBed.tick();
      http
        .expectOne(SEARCH_URL)
        .flush({ items: existing === null ? [] : [existing], totalCount: existing === null ? 0 : 1 });
    });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
  }

  it('mode_WithNoStatusOfTheCode_IsCreateWithTheDraftValues', async () => {
    const { store, http } = setup();
    await load(http, null);

    expect(store.mode()).toBe('create');
    expect(store.createValue()).toEqual({ code: 'e12', severity: 'error', description: null });
  });

  it('mode_WhenTheLoadedDraftsLackTheDraft_IsMissingAndNotLoading', async () => {
    const { store, http } = setup();
    TestBed.tick();

    http.expectOne(DRAFTS_URL).flush({ groups: [] });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(store.mode()).toBe('missing');
    expect(store.isLoading()).toBe(false);
  });

  it('search_ForTheCode_SortsByCodeSoTheExactCodeComesFirst', async () => {
    const { http } = setup();
    TestBed.tick();
    http.expectOne(DRAFTS_URL).flush(DRAFTS);
    let body: unknown;
    await vi.waitFor(() => {
      TestBed.tick();
      const request = http.expectOne(SEARCH_URL);
      body = request.request.body;
      request.flush({ items: [], totalCount: 0 });
    });

    expect(body).toEqual({
      offset: 0,
      limit: 25,
      sort: { column: 'code', direction: 'asc' },
      filter: { code: { value: 'e12' } },
    });
  });

  it('mode_WithStatusOfTheSameCodeInAnotherCase_IsUpdate', async () => {
    const { store, http } = setup();
    await load(http, EXISTING);

    expect(store.mode()).toBe('update');
    expect(store.updateDisabledReason()).toBe('DRAFTS.STATUS.NO_CHANGES');
  });

  it('saveCreate_AfterSuccess_MarksTheDraftAppliedAndCompletesChanged', async () => {
    const { store, http, actions } = setup();
    await load(http, null);

    store.saveCreate({ code: ' e12 ', severity: 'error' as never, description: null });
    http
      .expectOne('/api/telemetry/v1/machines/machine-1/status-definitions')
      .flush({ id: 'new' }, { status: HttpStatusCode.Created, statusText: 'Created' });
    const mark = http.expectOne(MARK_URL);
    mark.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });

    expect(mark.request.body).toEqual({ ids: ['draft-1'] });
    expect(actions.complete).toHaveBeenCalledWith(DraftActionType.Changed);
  });

  it('saveCreate_WhenTheSaveFails_MarksNothing', async () => {
    const { store, http, actions } = setup();
    await load(http, null);

    store.saveCreate({ code: 'e12', severity: 'error' as never, description: null });
    http
      .expectOne('/api/telemetry/v1/machines/machine-1/status-definitions')
      .flush(null, { status: HttpStatusCode.Conflict, statusText: 'Conflict' });

    http.expectNone(MARK_URL);
    expect(actions.complete).not.toHaveBeenCalled();
  });

  it('saveCreate_WhenTheMarkFails_ShowsAToastAndStillCompletesChanged', async () => {
    const { store, http, actions, messages } = setup();
    await load(http, null);

    store.saveCreate({ code: 'e12', severity: 'error' as never, description: null });
    http
      .expectOne('/api/telemetry/v1/machines/machine-1/status-definitions')
      .flush({ id: 'new' }, { status: HttpStatusCode.Created, statusText: 'Created' });
    http.expectOne(MARK_URL).flush(null, { status: HttpStatusCode.InternalServerError, statusText: 'Error' });

    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'DRAFTS.STATUS.MARK_FAILED' }));
    expect(actions.complete).toHaveBeenCalledWith(DraftActionType.Changed);
  });

  it('saveUpdate_WithSeverityTaken_UpdatesTheExistingStatusKeepingTheCodeThenMarks', async () => {
    const { store, http } = setup();
    await load(http, EXISTING);
    store.take(StatusCompareField.Severity, true);

    store.saveUpdate();
    const update = http.expectOne('/api/telemetry/v1/machines/machine-1/status-definitions/status-1');
    update.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
    http.expectOne(MARK_URL).flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });

    expect(update.request.method).toBe('PUT');
    expect(update.request.body).toEqual({ code: 'E12', severity: 'error', description: '<p>old</p>' });
  });
});
