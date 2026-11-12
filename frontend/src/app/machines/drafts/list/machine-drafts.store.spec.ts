import { ApplicationRef } from '@angular/core';
import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { patchState } from '@ngrx/signals';
import { unprotected } from '@ngrx/signals/testing';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { of } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';

import { DraftKind, DraftState, type MachineDraftGridItem } from '@machines/drafts/draft';
import { DraftActionType } from '@machines/drafts/draft-actions';
import { DraftsApi } from '@machines/drafts/drafts-api';
import { MetricDefinitionsApi } from '@machines/metrics/metric-definitions-api';
import { MetricUnitsApi } from '@machines/metrics/metric-units-api';
import { StatusDefinitionsApi } from '@machines/statuses/status-definitions-api';
import { MachineActionType } from '@machines/machine-actions';

import { MachineDraftsStore } from './machine-drafts.store';

const SEARCH_URL = '/api/core/v1/machines/machine-1/drafts/search';
const ATTEMPT_SEARCH_URL = '/api/core/v1/ingest-attempts/attempt-1/drafts/search';
const ALL_PENDING_COUNT = 52;
const DISMISS_ALL_URL = '/api/core/v1/machines/machine-1/drafts/dismiss';
const ATTEMPT_DISMISS_ALL_URL = '/api/core/v1/ingest-attempts/attempt-1/drafts/dismiss';
const LIST_LIMIT = 25;
const PENDING_PROBE_LIMIT = 1;
const ATTEMPT_DRAFTS_URL = '/api/core/v1/ingest-attempts/a1/drafts';
const ITEM: MachineDraftGridItem = {
  id: 'draft-1',
  kind: DraftKind.Metric,
  state: DraftState.Pending,
  attemptId: 'a1',
  fileId: 'f1',
  proposedAt: '2026-03-01T08:00:00Z',
  sourceLocation: 'page 1',
  label: 'Pressure',
  rowVersion: 1,
  isCreate: false,
  machineId: 'machine-1',
  machineName: 'Lathe',
};

const ALL_PENDING_PAGE_SIZE = 100;
const OPEN_DIALOG = { type: MachineActionType.Drafts, payload: { machineId: 'machine-1' } };

interface SetupOptions {
  readonly isConfirmed?: boolean;
  readonly isAttempt?: boolean;
}

describe('MachineDraftsStore', () => {
  function setup(options: SetupOptions = {}): {
    store: InstanceType<typeof MachineDraftsStore>;
    http: HttpTestingController;
    actions: {
      complete: ReturnType<typeof vi.fn>;
      cancel: ReturnType<typeof vi.fn>;
      dispatchChild: ReturnType<typeof vi.fn>;
      rememberReturn: ReturnType<typeof vi.fn>;
    };
  } {
    const actions = {
      complete: vi.fn(),
      cancel: vi.fn(),
      dispatchChild: vi.fn(() => Promise.resolve()),
      active: vi.fn(() => OPEN_DIALOG),
      rememberReturn: vi.fn(),
    };
    TestBed.configureTestingModule({
      providers: [
        MachineDraftsStore,
        DraftsApi,
        MetricDefinitionsApi,
        { provide: MetricUnitsApi, useValue: { getUnits: () => of({ kinds: [] }) } },
        StatusDefinitionsApi,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActionStore, useValue: actions },
        {
          provide: AuthStore,
          useValue: { hasPermission: (permission: string) => permission === Permission.DraftsWrite },
        },
        { provide: MessageService, useValue: { add: vi.fn() } },
        {
          provide: ConfirmationService,
          useValue: {
            confirm: vi.fn((confirmation: { accept?: () => void }) =>
              options.isConfirmed === true ? confirmation.accept?.() : undefined,
            ),
          },
        },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
      ],
    });
    const store = TestBed.inject(MachineDraftsStore);
    store.start(
      options.isAttempt === true
        ? { type: 'files.ingestSummary', payload: { attemptId: 'attempt-1' } }
        : { type: MachineActionType.Drafts, payload: { machineId: 'machine-1' } },
    );
    return { store, http: TestBed.inject(HttpTestingController), actions };
  }

  /** Answers the list request and the pending count request that every load sends together. */
  function answerSearches(
    http: HttpTestingController,
    items: readonly MachineDraftGridItem[],
    pendingCount: number,
    url: string = SEARCH_URL,
  ): void {
    const requests = http.match((candidate) => candidate.url === url);
    expect(requests).toHaveLength(2);
    for (const request of requests) {
      if (request.request.body.limit === PENDING_PROBE_LIMIT) {
        expect(request.request.body.filter.states).toEqual({ values: ['pending'] });
        request.flush({ items: [], totalCount: pendingCount });
      } else {
        request.flush({ items, totalCount: items.length });
      }
    }
  }

  async function answerAttempts(http: HttpTestingController): Promise<void> {
    await vi.waitFor(() => {
      TestBed.tick();
      http.expectOne(ATTEMPT_DRAFTS_URL).flush({ groups: [] });
    });
  }

  async function setup_loaded(options: SetupOptions = {}): Promise<ReturnType<typeof setup>> {
    const result = setup(options);
    await load(result.http, result.store, options.isAttempt === true);
    return result;
  }

  async function load(
    http: HttpTestingController,
    store: InstanceType<typeof MachineDraftsStore>,
    isAttempt = false,
  ): Promise<void> {
    TestBed.tick();
    answerSearches(http, [ITEM], ALL_PENDING_COUNT, isAttempt ? ATTEMPT_SEARCH_URL : SEARCH_URL);
    TestBed.tick();
    await answerAttempts(http);
    await TestBed.inject(ApplicationRef).whenStable();
    expect(store.drafts.value()?.totalCount).toBe(1);
  }

  it('search_WithKindAndStateFilter_SendsBothSetsAndThePendingFirstSort', async () => {
    const { store, http } = setup();
    store.setFilter({ kinds: [DraftKind.Status], states: [DraftState.Dismissed] });
    TestBed.tick();

    const request = http.expectOne((candidate) => candidate.url === SEARCH_URL && candidate.body.limit === LIST_LIMIT);

    expect(request.request.body).toEqual({
      offset: 0,
      limit: 25,
      sort: { column: 'state', direction: 'asc' },
      filter: { kinds: { values: ['status'] }, states: { values: ['dismissed'] } },
    });
    request.flush({ items: [], totalCount: 0 });
  });

  it('search_ForAnAttempt_UsesTheAttemptRouteForTheListAndForThePendingCount', async () => {
    const { store, http } = setup({ isAttempt: true });
    TestBed.tick();

    answerSearches(http, [], 0, ATTEMPT_SEARCH_URL);

    expect(store.scope()).toEqual({ kind: 'attempt', id: 'attempt-1' });
    http.expectNone(SEARCH_URL);
  });

  it('pendingCount_AfterTheLoad_HoldsTheCountOfPendingDraftsOfTheScope', async () => {
    const { store, http } = await setup_loaded();

    expect(store.pendingCount()).toBe(ALL_PENDING_COUNT);
    http.verify();
  });

  it('toggleSelected_ThenPageChange_KeepsTheChecksOfThePreviousPage', async () => {
    const { store, http } = await setup_loaded();

    store.toggleSelected(ITEM, true);
    store.setPage(LIST_LIMIT, LIST_LIMIT);
    TestBed.tick();
    http
      .expectOne((candidate) => candidate.url === SEARCH_URL && candidate.body.limit === LIST_LIMIT)
      .flush({ items: [], totalCount: 0 });

    expect(store.selectedIds()).toEqual([ITEM.id]);
    expect(store.selectedCount()).toBe(1);
  });

  it('requestDismissAll_WithCheckedDrafts_AsksForTheCheckedCountAndDismissesOnlyThoseOneByOne', async () => {
    const { store, http } = await setup_loaded({ isConfirmed: true });
    const second = { ...ITEM, id: 'draft-2' };
    store.toggleSelected(ITEM, true);
    store.toggleSelected(second, true);

    store.requestDismissAll();
    http.expectOne('/api/core/v1/drafts/draft-1/dismiss').flush(null, { status: 204, statusText: 'No Content' });
    await vi.waitFor(() => {
      http.expectOne('/api/core/v1/drafts/draft-2/dismiss').flush(null, { status: 204, statusText: 'No Content' });
    });
    TestBed.tick();

    http.expectNone(DISMISS_ALL_URL);
    expect(store.selectedCount()).toBe(0);
    expect(store.hasChanged()).toBe(true);
  });

  it('requestDismissAll_WhenTheSecondCheckedDraftFails_KeepsOnlyTheDraftThatIsNotDismissed', async () => {
    const { store, http } = await setup_loaded({ isConfirmed: true });
    const second = { ...ITEM, id: 'draft-2' };
    store.toggleSelected(ITEM, true);
    store.toggleSelected(second, true);

    store.requestDismissAll();
    http.expectOne('/api/core/v1/drafts/draft-1/dismiss').flush(null, { status: 204, statusText: 'No Content' });
    await vi.waitFor(() => {
      http
        .expectOne('/api/core/v1/drafts/draft-2/dismiss')
        .flush(null, { status: HttpStatusCode.Conflict, statusText: 'Conflict' });
    });
    TestBed.tick();

    expect(store.selectedIds()).toEqual(['draft-2']);
    expect(store.isDismissing()).toBe(false);
  });

  it('requestDismiss_WhenConfirmed_DismissesLoadsAgainAndCloseCompletesChanged', async () => {
    const { store, http, actions } = setup({ isConfirmed: true });
    await load(http, store);

    store.requestDismiss(ITEM);
    http
      .expectOne('/api/core/v1/drafts/draft-1/dismiss')
      .flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
    TestBed.tick();
    answerSearches(http, [], 0);
    store.close();

    expect(actions.complete).toHaveBeenCalledWith(DraftActionType.Changed);
  });

  it('requestDismissAll_WithAKindFilter_AsksWithTheCountOfAllPendingDraftsAndSendsNothing', async () => {
    const { store, http } = await setup_loaded();
    const confirmation = TestBed.inject(ConfirmationService).confirm as ReturnType<typeof vi.fn>;
    const instant = vi.spyOn(TestBed.inject(TranslateService), 'instant');
    store.setFilter({ kinds: [DraftKind.Machine], states: [] });
    TestBed.tick();
    http
      .expectOne((candidate) => candidate.url === SEARCH_URL && candidate.body.limit === LIST_LIMIT)
      .flush({ items: [], totalCount: 1 });

    store.requestDismissAll();

    expect(confirmation).toHaveBeenCalledOnce();
    expect(instant).toHaveBeenCalledWith('DRAFTS.DISMISS_ALL_MESSAGE', { count: ALL_PENDING_COUNT });
    expect(confirmation.mock.calls[0][0].rejectButtonProps).toEqual({ severity: 'secondary' });
    expect(confirmation.mock.calls[0][0].acceptButtonProps).toEqual({ severity: 'danger' });
    http.expectNone(DISMISS_ALL_URL);
  });

  it('requestDismissAll_WithoutPendingDrafts_AsksNothingAndLoadsAgain', async () => {
    const { store, http } = setup();
    TestBed.tick();
    answerSearches(http, [], 0);
    TestBed.tick();
    const confirmation = TestBed.inject(ConfirmationService).confirm as ReturnType<typeof vi.fn>;

    store.requestDismissAll();
    TestBed.tick();

    expect(store.drafts.isLoading()).toBe(true);
    expect(confirmation).not.toHaveBeenCalled();
  });

  it('requestDismissAll_WhenConfirmed_SendsOnePostShowsTheCountToastAndLoadsAgain', async () => {
    const { store, http } = setup({ isConfirmed: true });
    await load(http, store);
    const messages = TestBed.inject(MessageService).add as ReturnType<typeof vi.fn>;

    store.requestDismissAll();
    http.expectOne(DISMISS_ALL_URL).flush({ dismissedCount: ALL_PENDING_COUNT });
    TestBed.tick();

    answerSearches(http, [], 0);
    expect(messages).toHaveBeenCalledOnce();
    expect(messages.mock.calls[0][0].severity).toBe('success');
    expect(messages.mock.calls[0][0].detail).toBe('DRAFTS.TOASTS.DISMISSED_ALL');
    expect(store.isDismissing()).toBe(false);
  });

  it('requestDismissAll_ForAnAttempt_PostsToTheAttemptRouteOnly', async () => {
    const { store, http } = await setup_loaded({ isConfirmed: true, isAttempt: true });

    store.requestDismissAll();

    http.expectOne(ATTEMPT_DISMISS_ALL_URL).flush({ dismissedCount: ALL_PENDING_COUNT });
    http.expectNone(DISMISS_ALL_URL);
  });

  it('requestDismissAll_WhenTheServerAnswersConflict_ShowsTheConflictToastAndLoadsAgain', async () => {
    const { store, http } = setup({ isConfirmed: true });
    await load(http, store);
    const messages = TestBed.inject(MessageService).add as ReturnType<typeof vi.fn>;

    store.requestDismissAll();
    http
      .expectOne(DISMISS_ALL_URL)
      .flush(
        { detail: 'The draft or the machine changed since you read it. Reload it and try again.' },
        { status: HttpStatusCode.Conflict, statusText: 'Conflict' },
      );
    TestBed.tick();

    answerSearches(http, [ITEM], ALL_PENDING_COUNT);
    expect(messages.mock.calls.at(-1)?.[0].detail).toBe('DRAFTS.CONFLICTS.DRAFT_CHANGED');
    expect(store.isDismissing()).toBe(false);
  });

  it('quickRun_AfterAnAppliedRun_LoadsTheDraftsAndTheAttemptsAgainAndCloseCompletesChanged', async () => {
    const { store, http, actions } = await setup_loaded();
    patchState(unprotected(store), { quickRun: { count: 1, hasApplied: true } });

    TestBed.tick();
    answerSearches(http, [ITEM], ALL_PENDING_COUNT);
    TestBed.tick();
    await answerAttempts(http);
    store.close();

    expect(actions.complete).toHaveBeenCalledWith(DraftActionType.Changed);
  });

  it('drafts_WhenTheLoadFails_ReportsTheErrorAndNotAnEmptyListAndRetryLoadsAgain', async () => {
    const { store, http } = setup();
    TestBed.tick();

    for (const request of http.match(SEARCH_URL)) {
      request.flush(null, { status: HttpStatusCode.TooManyRequests, statusText: 'Too Many Requests' });
    }
    await vi.waitFor(() => {
      TestBed.tick();
      expect(store.drafts.status()).toBe('error');
    });
    const hasErrorAfterFailure = store.hasLoadError();
    store.retryLoad();
    await vi.waitFor(() => {
      TestBed.tick();
      http.expectOne(SEARCH_URL).flush({ items: [ITEM], totalCount: 1 });
    });
    TestBed.tick();
    await answerAttempts(http);

    expect(hasErrorAfterFailure).toBe(true);
    expect(store.hasLoadError()).toBe(false);
    expect(store.drafts.value()?.totalCount).toBe(1);
  });

  it('requestApplyAll_WhenEveryGroupOnThePageNeedsReview_StillReadsEveryPageOfTheMachine', async () => {
    const { store, http } = setup();
    TestBed.tick();
    answerSearches(http, [ITEM], 1);
    TestBed.tick();
    await vi.waitFor(() => {
      TestBed.tick();
      http.expectOne(ATTEMPT_DRAFTS_URL).flush({
        groups: [
          {
            machineId: 'machine-1',
            machineName: 'Lathe',
            machineDraft: null,
            metrics: [
              {
                id: 'draft-1',
                state: 'pending',
                sourceLocation: null,
                key: 'pressure',
                name: 'Pressure',
                description: null,
                valueKind: 'numeric',
                quantityKindName: 'Pressure',
                unitName: 'Parsec',
                min: null,
                max: null,
                states: null,
              },
            ],
            statuses: [],
          },
        ],
      });
    });
    await TestBed.inject(ApplicationRef).whenStable();

    store.requestApplyAll();
    TestBed.tick();

    const search = http.expectOne(
      (candidate) => candidate.url === SEARCH_URL && candidate.body.limit === ALL_PENDING_PAGE_SIZE,
    );
    expect(search.request.body.offset).toBe(0);
    expect(search.request.body.filter.states).toEqual({ values: ['pending'] });
    search.flush({ items: [], totalCount: 0 });
  });

  it('requestApplyAll_ForAnAttempt_RunsTheAttemptAndReadsNoMachinePages', async () => {
    const { store, http } = await setup_loaded({ isAttempt: true });

    store.requestApplyAll();

    await vi.waitFor(() => {
      TestBed.tick();
      expect(store.quickRun().count).toBe(1);
    });
    http.expectNone(SEARCH_URL);
  });

  it('close_WithoutChanges_CancelsTheAction', async () => {
    const { store, http, actions } = setup();
    await load(http, store);

    store.close();

    expect(actions.cancel).toHaveBeenCalledTimes(1);
    expect(actions.complete).not.toHaveBeenCalled();
  });

  it('requestReview_ForMetricDraft_KeepsTheDialogForTheReturnAndOpensTheMetricsPage', async () => {
    const { store, http, actions } = setup();
    await load(http, store);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    store.requestReview({ ...ITEM, kind: DraftKind.Metric });

    expect(actions.rememberReturn).toHaveBeenCalledWith(expect.any(String), OPEN_DIALOG, '/machines/machine-1/metrics');
    expect(actions.cancel).toHaveBeenCalledTimes(1);
    expect(navigate).toHaveBeenCalledWith(['/machines', 'machine-1', 'metrics'], {
      queryParams: { draftAttemptId: ITEM.attemptId },
    });
  });

  it('requestReview_ForMetricDraftOfAPendingCreateDraft_OpensNothing', async () => {
    const { store, http, actions } = setup();
    await load(http, store);

    store.requestReview({ ...ITEM, kind: DraftKind.Metric, machineId: null });

    expect(actions.dispatchChild).not.toHaveBeenCalled();
    expect(actions.cancel).not.toHaveBeenCalled();
  });

  it.each([
    [true, MachineActionType.Create],
    [false, DraftActionType.CompareMachine],
  ])('requestReview_ForMachineDraftWithIsCreate%s_Opens%s', async (isCreate, type) => {
    const { store, http, actions } = setup();
    await load(http, store);

    store.requestReview({ ...ITEM, kind: DraftKind.Machine, isCreate, machineId: isCreate ? null : 'machine-1' });

    expect(actions.dispatchChild).toHaveBeenCalledWith({ type, payload: { draftId: ITEM.id } });
  });

  it('requestReview_ForStatusDraftOfAKnownMachine_OpensTheStatusReviewWithTheAttempt', async () => {
    const { store, http, actions } = setup();
    await load(http, store);

    store.requestReview({ ...ITEM, id: 'draft-5', kind: DraftKind.Status });

    expect(actions.dispatchChild).toHaveBeenCalledWith({
      type: DraftActionType.ReviewStatus,
      payload: { machineId: 'machine-1', attemptId: 'a1', draftId: 'draft-5' },
    });
  });
});
