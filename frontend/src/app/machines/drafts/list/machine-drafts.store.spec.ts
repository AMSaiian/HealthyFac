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
import { AuthStore } from '@shared/security/auth.store';
import { Permission } from '@shared/security/permissions';

import { DraftKind } from '@machines/drafts/draft';
import { DraftActionType } from '@machines/drafts/draft-actions';
import { DraftsApi } from '@machines/drafts/drafts-api';
import { MetricDefinitionsApi } from '@machines/metrics/metric-definitions-api';
import { MetricUnitsApi } from '@machines/metrics/metric-units-api';
import { StatusDefinitionsApi } from '@machines/statuses/status-definitions-api';
import { MachineActionType } from '@machines/machine-actions';

import { MachineDraftsStore } from './machine-drafts.store';

const SEARCH_URL = '/api/core/v1/machines/machine-1/drafts/search';
const ALL_PENDING_COUNT = 52;
const DISMISS_ALL_URL = '/api/core/v1/machines/machine-1/drafts/dismiss';
const ITEM = {
  id: 'draft-1',
  kind: DraftKind.Metric,
  attemptId: 'a1',
  fileId: 'f1',
  proposedAt: '2026-03-01T08:00:00Z',
  sourceLocation: 'page 1',
  label: 'Pressure',
  rowVersion: 1,
};

const ALL_PENDING_PAGE_SIZE = 100;
const OPEN_DIALOG = { type: MachineActionType.Drafts, payload: { machineId: 'machine-1' } };

describe('MachineDraftsStore', () => {
  function setup(options: { isConfirmed?: boolean } = {}): {
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
    store.start({ type: MachineActionType.Drafts, payload: { machineId: 'machine-1' } });
    return { store, http: TestBed.inject(HttpTestingController), actions };
  }

  async function setup_loaded(): Promise<ReturnType<typeof setup>> {
    const result = setup();
    await load(result.http, result.store);
    return result;
  }

  async function load(http: HttpTestingController, store: InstanceType<typeof MachineDraftsStore>): Promise<void> {
    TestBed.tick();
    http.expectOne(SEARCH_URL).flush({ items: [ITEM], totalCount: 1 });
    TestBed.tick();
    await vi.waitFor(() => {
      TestBed.tick();
      http.expectOne('/api/core/v1/ingest-attempts/a1/drafts').flush({ groups: [] });
    });
    await TestBed.inject(ApplicationRef).whenStable();
    expect(store.drafts.value()?.totalCount).toBe(1);
  }

  it('search_WithKindFilter_SendsTheKindsAndTheNewestFirstSort', async () => {
    const { store, http } = setup();
    store.setFilter({ kinds: [DraftKind.Status] });
    TestBed.tick();

    const request = http.expectOne(SEARCH_URL);

    expect(request.request.body).toEqual({
      offset: 0,
      limit: 25,
      sort: { column: 'proposedAt', direction: 'desc' },
      filter: { kinds: { values: ['status'] } },
    });
    request.flush({ items: [], totalCount: 0 });
  });

  it('requestDismiss_WhenConfirmed_DismissesLoadsAgainAndCloseCompletesChanged', async () => {
    const { store, http, actions } = setup({ isConfirmed: true });
    await load(http, store);

    store.requestDismiss(ITEM);
    http
      .expectOne('/api/core/v1/drafts/draft-1/dismiss')
      .flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
    TestBed.tick();
    http.expectOne(SEARCH_URL).flush({ items: [], totalCount: 0 });
    store.close();

    expect(actions.complete).toHaveBeenCalledWith(DraftActionType.Changed);
  });

  function answerCount(http: HttpTestingController, total: number): void {
    const request = http.expectOne((candidate) => candidate.url === SEARCH_URL && candidate.body?.limit === 1);
    expect(request.request.body.filter).toEqual({});
    request.flush({ items: [], totalCount: total });
  }

  it('requestDismissAll_WithAKindFilter_AsksWithTheUnfilteredCountAndSendsNothing', async () => {
    const { store, http } = await setup_loaded();
    const confirmation = TestBed.inject(ConfirmationService).confirm as ReturnType<typeof vi.fn>;
    const instant = vi.spyOn(TestBed.inject(TranslateService), 'instant');
    store.setFilter({ kinds: [DraftKind.Machine] });
    TestBed.tick();
    http
      .expectOne((candidate) => candidate.url === SEARCH_URL && candidate.body.limit === 25)
      .flush({
        items: [],
        totalCount: 1,
      });

    store.requestDismissAll();
    answerCount(http, ALL_PENDING_COUNT);

    expect(confirmation).toHaveBeenCalledOnce();
    expect(instant).toHaveBeenCalledWith('DRAFTS.DISMISS_ALL_MESSAGE', { count: ALL_PENDING_COUNT });
    expect(confirmation.mock.calls[0][0].rejectButtonProps).toEqual({ severity: 'secondary' });
    expect(confirmation.mock.calls[0][0].acceptButtonProps).toEqual({ severity: 'danger' });
    http.expectNone(DISMISS_ALL_URL);
  });

  it('requestDismissAll_WithoutPendingDrafts_AsksNothingAndLoadsAgain', async () => {
    const { store, http } = await setup_loaded();
    const confirmation = TestBed.inject(ConfirmationService).confirm as ReturnType<typeof vi.fn>;

    store.requestDismissAll();
    answerCount(http, 0);
    TestBed.tick();

    http
      .expectOne((candidate) => candidate.url === SEARCH_URL && candidate.body.limit === 25)
      .flush({
        items: [],
        totalCount: 0,
      });
    expect(confirmation).not.toHaveBeenCalled();
  });

  it('requestDismissAll_WhenConfirmed_SendsOnePostShowsTheCountToastAndLoadsAgain', async () => {
    const { store, http } = setup({ isConfirmed: true });
    await load(http, store);
    const messages = TestBed.inject(MessageService).add as ReturnType<typeof vi.fn>;

    store.requestDismissAll();
    answerCount(http, ALL_PENDING_COUNT);
    http.expectOne(DISMISS_ALL_URL).flush({ dismissedCount: ALL_PENDING_COUNT });
    TestBed.tick();

    http
      .expectOne((candidate) => candidate.url === SEARCH_URL && candidate.body.limit === 25)
      .flush({
        items: [],
        totalCount: 0,
      });
    expect(messages).toHaveBeenCalledOnce();
    expect(messages.mock.calls[0][0].severity).toBe('success');
    expect(messages.mock.calls[0][0].detail).toBe('DRAFTS.TOASTS.DISMISSED_ALL');
    expect(store.isDismissing()).toBe(false);
  });

  it('requestDismissAll_WhenTheServerAnswersConflict_ShowsTheConflictToastAndLoadsAgain', async () => {
    const { store, http } = setup({ isConfirmed: true });
    await load(http, store);
    const messages = TestBed.inject(MessageService).add as ReturnType<typeof vi.fn>;

    store.requestDismissAll();
    answerCount(http, ALL_PENDING_COUNT);
    http
      .expectOne(DISMISS_ALL_URL)
      .flush(
        { detail: 'The draft or the machine changed since you read it. Reload it and try again.' },
        { status: HttpStatusCode.Conflict, statusText: 'Conflict' },
      );
    TestBed.tick();

    http
      .expectOne((candidate) => candidate.url === SEARCH_URL && candidate.body.limit === 25)
      .flush({
        items: [ITEM],
        totalCount: 1,
      });
    expect(messages.mock.calls.at(-1)?.[0].detail).toBe('DRAFTS.CONFLICTS.DRAFT_CHANGED');
    expect(store.isDismissing()).toBe(false);
  });

  it('quickRun_AfterAnAppliedRun_LoadsTheDraftsAndTheAttemptsAgainAndCloseCompletesChanged', async () => {
    const { store, http, actions } = await setup_loaded();
    patchState(unprotected(store), { quickRun: { count: 1, hasApplied: true } });

    TestBed.tick();
    http.expectOne(SEARCH_URL).flush({ items: [ITEM], totalCount: 1 });
    TestBed.tick();
    await vi.waitFor(() => {
      TestBed.tick();
      http.expectOne('/api/core/v1/ingest-attempts/a1/drafts').flush({ groups: [] });
    });
    store.close();

    expect(actions.complete).toHaveBeenCalledWith(DraftActionType.Changed);
  });

  it('drafts_WhenTheLoadFails_ReportsTheErrorAndNotAnEmptyListAndRetryLoadsAgain', async () => {
    const { store, http } = setup();
    TestBed.tick();

    http.expectOne(SEARCH_URL).flush(null, { status: HttpStatusCode.TooManyRequests, statusText: 'Too Many Requests' });
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
    await vi.waitFor(() => {
      TestBed.tick();
      http.expectOne('/api/core/v1/ingest-attempts/a1/drafts').flush({ groups: [] });
    });

    expect(hasErrorAfterFailure).toBe(true);
    expect(store.hasLoadError()).toBe(false);
    expect(store.drafts.value()?.totalCount).toBe(1);
  });

  it('requestApplyAll_WhenEveryGroupOnThePageNeedsReview_StillReadsEveryPageOfTheMachine', async () => {
    const { store, http } = setup();
    TestBed.tick();
    http.expectOne(SEARCH_URL).flush({ items: [ITEM], totalCount: 1 });
    TestBed.tick();
    await vi.waitFor(() => {
      TestBed.tick();
      http.expectOne('/api/core/v1/ingest-attempts/a1/drafts').flush({
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
    search.flush({ items: [], totalCount: 0 });
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

  it('requestReview_ForMachineDraft_OpensItAsAChild', async () => {
    const { store, http, actions } = setup();
    await load(http, store);

    store.requestReview({ ...ITEM, kind: DraftKind.Machine });

    expect(actions.dispatchChild).toHaveBeenCalledWith({
      type: DraftActionType.CompareMachine,
      payload: { draftId: ITEM.id },
    });
  });
});
