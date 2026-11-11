import { ApplicationRef } from '@angular/core';
import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { of } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import { AuthStore } from '@shared/security/auth.store';
import { Permission } from '@shared/security/permissions';

import { FileActionType } from '@files/file-actions';
import { IngestApi } from '@files/ingest/ingest-api';
import { IngestState } from '@files/ingest/ingest-attempt';
import { DraftKind, DraftState } from '@machines/drafts/draft';
import { DraftActionType } from '@machines/drafts/draft-actions';
import { MachineActionType } from '@machines/machine-actions';
import { DraftsApi } from '@machines/drafts/drafts-api';
import { MetricDefinitionsApi } from '@machines/metrics/metric-definitions-api';
import { MetricUnitsApi } from '@machines/metrics/metric-units-api';
import { StatusDefinitionsApi } from '@machines/statuses/status-definitions-api';

import { IngestSummaryStore } from './ingest-summary.store';

const ATTEMPT_URL = '/api/insights/v1/ingest-attempts/attempt-id';
const DRAFTS_URL = '/api/core/v1/ingest-attempts/attempt-id/drafts';

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

const DRAFTS = {
  groups: [
    {
      machineId: 'machine-1',
      machineName: 'Press',
      machineDraft: null,
      metrics: [
        {
          id: 'metric-draft',
          state: 'pending',
          sourceLocation: 'page 1',
          key: 'inlet.pressure',
          name: null,
          description: null,
          valueKind: 'numeric',
          quantityKindName: null,
          unitName: null,
          min: null,
          max: 6,
          states: null,
        },
      ],
      statuses: [],
    },
  ],
};

const OPEN_DIALOG = { type: FileActionType.IngestSummary, payload: { fileId: 'file-1' } };

describe('IngestSummaryStore', () => {
  function setup(options: { isConfirmed?: boolean } = {}): {
    store: InstanceType<typeof IngestSummaryStore>;
    http: HttpTestingController;
    actions: {
      complete: ReturnType<typeof vi.fn>;
      cancel: ReturnType<typeof vi.fn>;
      dispatchChild: ReturnType<typeof vi.fn>;
      active: ReturnType<typeof vi.fn>;
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
        IngestSummaryStore,
        IngestApi,
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
    const store = TestBed.inject(IngestSummaryStore);
    store.start({ type: FileActionType.IngestSummary, payload: { attemptId: 'attempt-id' } });
    return { store, http: TestBed.inject(HttpTestingController), actions };
  }

  async function settle(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
  }

  it('load_WithAttemptAndDrafts_ShowsTheReasonAndTheGroups', async () => {
    const { store, http } = setup();
    TestBed.tick();

    http.expectOne(ATTEMPT_URL).flush(ATTEMPT);
    http.expectOne(DRAFTS_URL).flush(DRAFTS);
    await settle();

    expect(store.reasonKey()).toBe('INGEST.ERRORS.NO_TEXT');
    expect(store.groups()).toHaveLength(1);
    expect(store.groups()[0].rows[0].isReviewDisabled).toBe(false);
    expect(store.canWrite()).toBe(true);
  });

  it('load_WithUnknownAttempt_ShowsNotFoundAndNoGroups', async () => {
    const { store, http } = setup();
    TestBed.tick();

    http.expectOne(ATTEMPT_URL).flush(null, { status: HttpStatusCode.NotFound, statusText: 'Not Found' });
    http.expectOne(DRAFTS_URL).flush(DRAFTS);
    await settle();

    expect(store.isNotFound()).toBe(true);
  });

  it('requestDismiss_WhenConfirmed_DismissesAndLoadsTheDraftsAgain', async () => {
    const { store, http } = setup({ isConfirmed: true });
    TestBed.tick();
    http.expectOne(ATTEMPT_URL).flush(ATTEMPT);
    http.expectOne(DRAFTS_URL).flush(DRAFTS);
    await settle();

    store.requestDismiss(store.groups()[0].rows[0]);
    http.expectOne('/api/core/v1/drafts/metric-draft/dismiss').flush(null, {
      status: HttpStatusCode.NoContent,
      statusText: 'No Content',
    });
    TestBed.tick();

    http.expectOne(DRAFTS_URL).flush({ groups: [] });
    await settle();

    expect(store.groups()).toEqual([]);
    expect(store.hasChanged()).toBe(true);
  });

  it('requestDismiss_WhenNotConfirmed_SendsNothing', async () => {
    const { store, http } = setup();
    TestBed.tick();
    http.expectOne(ATTEMPT_URL).flush(ATTEMPT);
    http.expectOne(DRAFTS_URL).flush(DRAFTS);
    await settle();

    store.requestDismiss(store.groups()[0].rows[0]);

    http.expectNone('/api/core/v1/drafts/metric-draft/dismiss');
    expect(store.hasChanged()).toBe(false);
  });

  it('close_WithoutChanges_CancelsTheAction', () => {
    const { store, actions } = setup();

    store.close();

    expect(actions.cancel).toHaveBeenCalledTimes(1);
    expect(actions.complete).not.toHaveBeenCalled();
  });

  it('close_AfterADismiss_CompletesWithDraftsChanged', async () => {
    const { store, http, actions } = setup({ isConfirmed: true });
    TestBed.tick();
    http.expectOne(ATTEMPT_URL).flush(ATTEMPT);
    http.expectOne(DRAFTS_URL).flush(DRAFTS);
    await settle();
    store.requestDismiss(store.groups()[0].rows[0]);
    http.expectOne('/api/core/v1/drafts/metric-draft/dismiss').flush(null, {
      status: HttpStatusCode.NoContent,
      statusText: 'No Content',
    });

    store.close();

    expect(actions.complete).toHaveBeenCalledWith(DraftActionType.Changed);
    expect(actions.cancel).not.toHaveBeenCalled();
  });

  it.each([
    [true, MachineActionType.Create],
    [false, DraftActionType.CompareMachine],
  ])('requestReview_ForMachineDraftWithIsCreate%s_Opens%s', (isCreate, type) => {
    const { store, actions } = setup();

    store.requestReview({
      id: 'draft-9',
      kind: DraftKind.Machine,
      state: DraftState.Pending,
      isCreate,
      label: 'Pump',
      sourceLocation: null,
      isPending: true,
      machineId: null,
      isReviewDisabled: false,
    });

    expect(actions.dispatchChild).toHaveBeenCalledWith({ type, payload: { draftId: 'draft-9' } });
  });

  it('requestReview_ForStatusDraftOfAKnownMachine_OpensTheStatusReviewWithTheAttempt', () => {
    const { store, actions } = setup();

    store.requestReview({
      id: 'draft-5',
      kind: DraftKind.Status,
      state: DraftState.Pending,
      isCreate: false,
      label: 'E12',
      sourceLocation: 'page 2',
      isPending: true,
      machineId: 'machine-1',
      isReviewDisabled: false,
    });

    expect(actions.dispatchChild).toHaveBeenCalledWith({
      type: DraftActionType.ReviewStatus,
      payload: { machineId: 'machine-1', attemptId: 'attempt-id', draftId: 'draft-5' },
    });
  });

  it('requestReview_ForMetricDraft_KeepsTheDialogForTheReturnAndOpensTheMetricsPage', () => {
    const { store, actions } = setup();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    store.requestReview({
      id: 'draft-6',
      kind: DraftKind.Metric,
      state: DraftState.Pending,
      isCreate: false,
      label: 'Pressure',
      sourceLocation: null,
      isPending: true,
      machineId: 'machine-1',
      isReviewDisabled: false,
    });

    expect(actions.rememberReturn).toHaveBeenCalledWith(expect.any(String), OPEN_DIALOG, '/machines/machine-1/metrics');
    expect(actions.cancel).toHaveBeenCalledTimes(1);
    expect(navigate).toHaveBeenCalledWith(['/machines', 'machine-1', 'metrics'], {
      queryParams: { draftAttemptId: 'attempt-id' },
    });
  });
});
