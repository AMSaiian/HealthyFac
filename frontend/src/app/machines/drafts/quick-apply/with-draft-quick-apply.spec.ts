import { type HttpRequest, HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { signalStore } from '@ngrx/signals';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';

import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';

import { DraftState } from '@machines/drafts/draft';
import { DraftsApi } from '@machines/drafts/drafts-api';
import { MetricDefinitionsApi } from '@machines/metrics/metric-definitions-api';
import { MetricUnitsApi } from '@machines/metrics/metric-units-api';
import { StatusDefinitionsApi } from '@machines/statuses/status-definitions-api';

import { type AttemptDraftsEntry, withDraftQuickApply } from './with-draft-quick-apply';

const DRAFTS_URL = '/api/core/v1/ingest-attempts/a1/drafts';
const MACHINE_APPLY_URL = '/api/core/v1/machine-drafts/md/apply';
const MARK_URL = '/api/core/v1/drafts/mark-applied';
const DEFINITIONS_URL = '/api/telemetry/v1/machines/m1/metric-definitions';
const MACHINE_DRAFT_URL = '/api/core/v1/machine-drafts/md';
const STATUSES_URL = '/api/telemetry/v1/machines/m1/status-definitions';
const STATUSES_APPLY_URL = `${STATUSES_URL}/apply`;
const UNITS_URL = '/api/telemetry/v1/metric-units';

const UNITS = { kinds: [{ name: 'Temperature', units: [{ name: 'Celsius', abbreviation: '°C' }] }] };

const MACHINE_DRAFT = {
  id: 'md',
  state: DraftState.Pending,
  isCreate: true,
  attemptId: 'a1',
  fileId: 'f1',
  name: 'Lathe',
  serialNumber: 'S-1',
  description: null,
  rowVersion: 1,
};
const METRIC_DRAFT = {
  id: 'mt',
  state: DraftState.Pending,
  sourceLocation: null,
  key: 'spindle.temperature',
  name: 'Spindle temperature',
  description: null,
  valueKind: 'numeric',
  quantityKindName: 'Temperature',
  unitName: 'Celsius',
  min: null,
  max: null,
  states: null,
};
const STATUS_DRAFT = {
  id: 'st',
  state: DraftState.Pending,
  sourceLocation: null,
  code: 'E12',
  severity: 'warning',
  description: null,
};

const CREATE_GROUP = {
  machineId: null,
  machineName: null,
  machineDraft: MACHINE_DRAFT,
  metrics: [METRIC_DRAFT],
  statuses: [STATUS_DRAFT],
};
const MOVED_GROUP = {
  machineId: 'm1',
  machineName: 'Lathe',
  machineDraft: { ...MACHINE_DRAFT, state: DraftState.Applied },
  metrics: [METRIC_DRAFT],
  statuses: [STATUS_DRAFT],
};
const EXISTING_GROUP = { ...MOVED_GROUP, machineDraft: null };

const HostStore = signalStore(withDraftQuickApply());

async function untilRequest(
  http: HttpTestingController,
  url: string,
  method: string,
  body: unknown = null,
  errorStatus: number = HttpStatusCode.Conflict,
  errorHeaders: Record<string, string> = {},
): Promise<HttpRequest<unknown>> {
  let sent: HttpRequest<unknown> | null = null;
  await vi.waitFor(() => {
    const request = http.expectOne((candidate) => candidate.url === url && candidate.method === method);
    sent = request.request;
    if (body instanceof Error) {
      request.flush(null, { status: errorStatus, statusText: 'Error', headers: errorHeaders });
    } else {
      request.flush(body as object | null);
    }
  });
  return sent as unknown as HttpRequest<unknown>;
}

async function until(
  http: HttpTestingController,
  url: string,
  method: string,
  body: unknown = null,
  errorStatus: number = HttpStatusCode.Conflict,
  errorHeaders: Record<string, string> = {},
): Promise<unknown> {
  const request = await untilRequest(http, url, method, body, errorStatus, errorHeaders);
  return request.body;
}

interface Setup {
  store: InstanceType<typeof HostStore>;
  http: HttpTestingController;
  messages: { add: ReturnType<typeof vi.fn> };
}

async function setup(entries: readonly AttemptDraftsEntry[]): Promise<Setup> {
  const messages = { add: vi.fn() };
  TestBed.configureTestingModule({
    providers: [
      HostStore,
      DraftsApi,
      MetricDefinitionsApi,
      MetricUnitsApi,
      StatusDefinitionsApi,
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: MessageService, useValue: messages },
      { provide: TranslateService, useValue: { instant: (key: string) => key } },
    ],
  });
  const store = TestBed.inject(HostStore);
  const http = TestBed.inject(HttpTestingController);
  store.setQuickDrafts(entries);
  TestBed.tick();
  http.expectOne(UNITS_URL).flush(UNITS);
  await TestBed.inject(ApplicationRef).whenStable();
  return { store, http, messages };
}

describe('withDraftQuickApply', () => {
  it('applyDraft_ForAStatusDraft_SendsABatchOfOneItemAndMarksTheDraft', async () => {
    const { store, http, messages } = await setup([{ attemptId: 'a1', drafts: { groups: [EXISTING_GROUP] } }]);

    store.applyDraft('a1', 'st');
    await until(http, DRAFTS_URL, 'GET', { groups: [EXISTING_GROUP] });
    const batch = await until(http, STATUSES_APPLY_URL, 'POST');
    const marked = await until(http, MARK_URL, 'POST');

    expect(batch).toEqual({ items: [{ code: 'E12', severity: 'warning', description: null }] });
    expect(marked).toEqual({ ids: ['st'] });
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
    expect(messages.add).toHaveBeenCalledWith(
      expect.objectContaining({ severity: 'success', detail: 'DRAFTS.QUICK.APPLIED' }),
    );
    expect(store.applyingIds()).toEqual([]);
  });

  it('applyGroup_ForACreateGroup_AppliesTheMachineThenOneMetricCallThenOneStatusBatchThenOneMark', async () => {
    const { store, http, messages } = await setup([{ attemptId: 'a1', drafts: { groups: [CREATE_GROUP] } }]);
    const order: string[] = [];

    store.applyGroup('a1', { machineDraftId: 'md', machineId: null });
    await until(http, DRAFTS_URL, 'GET', { groups: [CREATE_GROUP] });
    await until(http, MACHINE_APPLY_URL, 'POST', { machineId: 'm1' });
    order.push('machine');
    await until(http, DRAFTS_URL, 'GET', { groups: [MOVED_GROUP] });
    order.push('reload');
    await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
    const metricBody = (await until(http, DEFINITIONS_URL, 'PUT', { revision: 4 })) as {
      created: unknown[];
      expectedRevision: number;
    };
    order.push('metrics');
    await until(http, STATUSES_APPLY_URL, 'POST');
    order.push('statuses');
    const marked = await until(http, MARK_URL, 'POST');
    order.push('mark');
    await TestBed.inject(ApplicationRef).whenStable();

    http.verify();
    expect(order).toEqual(['machine', 'reload', 'metrics', 'statuses', 'mark']);
    expect(metricBody.created).toHaveLength(1);
    expect(metricBody.expectedRevision).toBe(3);
    expect(marked).toEqual({ ids: ['mt', 'st'] });
    expect(messages.add).toHaveBeenCalledWith(
      expect.objectContaining({ severity: 'success', detail: 'DRAFTS.QUICK.APPLIED' }),
    );
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
  });

  it('applyGroup_WithThirtyStatusesAndTwentyMetrics_SendsOneMetricApplyOneStatusBatchAndOneMark', async () => {
    const metrics = Array.from({ length: 20 }, (_, index) => ({
      ...METRIC_DRAFT,
      id: `mt${index}`,
      key: `metric.${index}`,
    }));
    const statuses = Array.from({ length: 30 }, (_, index) => ({
      ...STATUS_DRAFT,
      id: `st${index}`,
      code: `E${index}`,
    }));
    const group = { ...EXISTING_GROUP, metrics, statuses };
    const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [group] } }]);

    store.applyGroup('a1', { machineDraftId: null, machineId: 'm1' });
    await until(http, DRAFTS_URL, 'GET', { groups: [group] });
    await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
    const metricBody = (await until(http, DEFINITIONS_URL, 'PUT', { revision: 4 })) as { created: unknown[] };
    const batch = (await until(http, STATUSES_APPLY_URL, 'POST')) as { items: unknown[] };
    const marked = (await until(http, MARK_URL, 'POST')) as { ids: string[] };
    await TestBed.inject(ApplicationRef).whenStable();

    http.verify();
    expect(metricBody.created).toHaveLength(20);
    expect(batch.items).toHaveLength(30);
    expect(marked.ids).toHaveLength(50);
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
  });

  it('applyGroup_WhenTheStatusBatchFails_MarksTheAppliedMetricsAndStops', async () => {
    const { store, http, messages } = await setup([{ attemptId: 'a1', drafts: { groups: [EXISTING_GROUP] } }]);

    store.applyGroup('a1', { machineDraftId: null, machineId: 'm1' });
    await until(http, DRAFTS_URL, 'GET', { groups: [EXISTING_GROUP] });
    await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
    await until(http, DEFINITIONS_URL, 'PUT', { revision: 4 });
    await until(http, STATUSES_APPLY_URL, 'POST', new Error('conflict'));
    const marked = await until(http, MARK_URL, 'POST');
    await TestBed.inject(ApplicationRef).whenStable();

    expect(marked).toEqual({ ids: ['mt'] });
    expect(messages.add).toHaveBeenCalledWith(
      expect.objectContaining({ severity: 'error', detail: 'DRAFTS.QUICK.FAILED' }),
    );
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
  });

  it('applyGroup_ForACreateGroupWhenTelemetryDoesNotKnowTheMachineYet_AsksAgainAndThenApplies', async () => {
    const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [CREATE_GROUP] } }]);
    const metricsOnly = { ...MOVED_GROUP, statuses: [] };

    store.applyGroup('a1', { machineDraftId: 'md', machineId: null });
    await until(http, DRAFTS_URL, 'GET', { groups: [CREATE_GROUP] });
    await until(http, MACHINE_APPLY_URL, 'POST', { machineId: 'm1' });
    await until(http, DRAFTS_URL, 'GET', { groups: [metricsOnly] });
    await until(http, DEFINITIONS_URL, 'GET', new Error('not known yet'), HttpStatusCode.NotFound);
    await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
    await until(http, DEFINITIONS_URL, 'PUT', { revision: 4 });
    const marked = await until(http, MARK_URL, 'POST');

    expect(marked).toEqual({ ids: ['mt'] });
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
  });

  it('applyDraft_ForAStatusWhenTelemetryDoesNotKnowTheMachineYet_AsksAgainAndThenApplies', async () => {
    const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [EXISTING_GROUP] } }]);

    store.applyDraft('a1', 'st');
    await until(http, DRAFTS_URL, 'GET', { groups: [EXISTING_GROUP] });
    await until(http, STATUSES_APPLY_URL, 'POST', new Error('not known yet'), HttpStatusCode.NotFound);
    await until(http, STATUSES_APPLY_URL, 'POST');
    await until(http, MARK_URL, 'POST');

    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
  });

  it('applyDraft_WhenTelemetryNeverKnowsTheMachine_WaitsHalfOneTwoFourEightSecondsThenStopsWithTheNotReadyToast', async () => {
    const { store, http, messages } = await setup([{ attemptId: 'a1', drafts: { groups: [EXISTING_GROUP] } }]);
    const translate = vi.spyOn(TestBed.inject(TranslateService), 'instant');
    const notFound = (): void => {
      http
        .expectOne((candidate) => candidate.url === STATUSES_APPLY_URL)
        .flush(null, { status: HttpStatusCode.NotFound, statusText: 'Not Found' });
    };
    vi.useFakeTimers();
    try {
      store.applyDraft('a1', 'st');
      await vi.advanceTimersByTimeAsync(0);
      http.expectOne(DRAFTS_URL).flush({ groups: [EXISTING_GROUP] });
      await vi.advanceTimersByTimeAsync(0);
      notFound();
      for (const delayMs of [500, 1000, 2000, 4000, 8000]) {
        await vi.advanceTimersByTimeAsync(delayMs - 1);
        http.expectNone(STATUSES_APPLY_URL);
        await vi.advanceTimersByTimeAsync(1);
        notFound();
      }
      await vi.advanceTimersByTimeAsync(0);

      http.expectNone(MARK_URL);
      expect(translate).toHaveBeenCalledWith('DRAFTS.QUICK.MACHINE_NOT_READY');
      expect(messages.add).toHaveBeenCalledWith(
        expect.objectContaining({ severity: 'error', detail: 'DRAFTS.QUICK.FAILED' }),
      );
      expect(store.quickRun()).toEqual({ count: 1, hasApplied: false });
    } finally {
      vi.useRealTimers();
    }
  });

  it('applyDraft_WhenTheGatewayAnswers429WithRetryAfter_WaitsThenSendsTheSameCallOnce', async () => {
    const { store, http, messages } = await setup([{ attemptId: 'a1', drafts: { groups: [EXISTING_GROUP] } }]);
    const translate = vi.spyOn(TestBed.inject(TranslateService), 'instant');
    const tooMany = (): void => {
      http
        .expectOne((candidate) => candidate.url === STATUSES_APPLY_URL)
        .flush(null, {
          status: HttpStatusCode.TooManyRequests,
          statusText: 'Too Many',
          headers: { 'Retry-After': '2' },
        });
    };
    vi.useFakeTimers();
    try {
      store.applyDraft('a1', 'st');
      await vi.advanceTimersByTimeAsync(0);
      http.expectOne(DRAFTS_URL).flush({ groups: [EXISTING_GROUP] });
      await vi.advanceTimersByTimeAsync(0);
      tooMany();
      await vi.advanceTimersByTimeAsync(1999);
      http.expectNone(STATUSES_APPLY_URL);
      await vi.advanceTimersByTimeAsync(1);
      http.expectOne(STATUSES_APPLY_URL).flush(null);
      await vi.advanceTimersByTimeAsync(0);
      http.expectOne(MARK_URL).flush(null);
      await vi.advanceTimersByTimeAsync(0);

      expect(translate).toHaveBeenCalledWith('DRAFTS.QUICK.WAITING', { seconds: 2 });
      expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'info' }));
      expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
    } finally {
      vi.useRealTimers();
    }
  });

  it('applyGroup_WhenTheGatewayAnswers429TwiceInARow_StopsAndLeavesTheRestPending', async () => {
    const { store, http, messages } = await setup([{ attemptId: 'a1', drafts: { groups: [EXISTING_GROUP] } }]);
    const translate = vi.spyOn(TestBed.inject(TranslateService), 'instant');
    const tooMany = (): void => {
      http
        .expectOne((candidate) => candidate.url === DEFINITIONS_URL)
        .flush(null, {
          status: HttpStatusCode.TooManyRequests,
          statusText: 'Too Many',
          headers: { 'Retry-After': '1' },
        });
    };
    vi.useFakeTimers();
    try {
      store.applyGroup('a1', { machineDraftId: null, machineId: 'm1' });
      await vi.advanceTimersByTimeAsync(0);
      http.expectOne(DRAFTS_URL).flush({ groups: [EXISTING_GROUP] });
      await vi.advanceTimersByTimeAsync(0);
      tooMany();
      await vi.advanceTimersByTimeAsync(1000);
      tooMany();
      await vi.advanceTimersByTimeAsync(0);

      http.expectNone(DEFINITIONS_URL);
      http.expectNone(STATUSES_APPLY_URL);
      http.expectNone(MARK_URL);
      expect(translate).toHaveBeenCalledWith('DRAFTS.QUICK.RATE_LIMITED');
      expect(messages.add).toHaveBeenCalledWith(
        expect.objectContaining({ severity: 'error', detail: 'DRAFTS.QUICK.FAILED' }),
      );
      expect(store.quickRun()).toEqual({ count: 1, hasApplied: false });
    } finally {
      vi.useRealTimers();
    }
  });

  it('applyGroup_WhenTheMetricApplyConflicts_StopsAndSendsNoStatus', async () => {
    const { store, http, messages } = await setup([{ attemptId: 'a1', drafts: { groups: [EXISTING_GROUP] } }]);

    store.applyGroup('a1', { machineDraftId: null, machineId: 'm1' });
    await until(http, DRAFTS_URL, 'GET', { groups: [EXISTING_GROUP] });
    await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
    await until(http, DEFINITIONS_URL, 'PUT', new Error('conflict'));
    await TestBed.inject(ApplicationRef).whenStable();

    http.expectNone(STATUSES_APPLY_URL);
    http.expectNone(MARK_URL);
    expect(messages.add).toHaveBeenCalledWith(
      expect.objectContaining({ severity: 'error', detail: 'DRAFTS.QUICK.FAILED' }),
    );
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: false });
    expect(store.applyingIds()).toEqual([]);
  });

  it('applyGroup_WithAMetricThatNeedsReview_SkipsItAndReportsTheCount', async () => {
    const badMetric = { ...METRIC_DRAFT, unitName: 'Parsec' };
    const group = { ...EXISTING_GROUP, metrics: [badMetric] };
    const { store, http, messages } = await setup([{ attemptId: 'a1', drafts: { groups: [group] } }]);

    store.applyGroup('a1', { machineDraftId: null, machineId: 'm1' });
    await until(http, DRAFTS_URL, 'GET', { groups: [group] });
    await until(http, STATUSES_APPLY_URL, 'POST');
    await until(http, MARK_URL, 'POST');
    await TestBed.inject(ApplicationRef).whenStable();

    http.expectNone(DEFINITIONS_URL);
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
    expect(messages.add).toHaveBeenCalledWith(
      expect.objectContaining({ severity: 'success', detail: 'DRAFTS.QUICK.APPLIED_SKIPPED' }),
    );
  });

  it('applyDraft_ForAMachineCreateDraft_AppliesItWithItsNameAndSerialNumber', async () => {
    const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [CREATE_GROUP] } }]);

    store.applyDraft('a1', 'md');
    await until(http, DRAFTS_URL, 'GET', { groups: [CREATE_GROUP] });
    const applied = await until(http, MACHINE_APPLY_URL, 'POST', { machineId: 'm1' });

    expect(applied).toEqual({ name: 'Lathe', serialNumber: 'S-1', description: null });
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
  });

  it('applyDraft_ForAMachineUpdateDraft_KeepsTheCurrentValuesForNullFieldsAndSendsTheRowVersion', async () => {
    const draft = { ...MACHINE_DRAFT, isCreate: false, name: null, serialNumber: 'S-2', description: null };
    const group = { ...EXISTING_GROUP, machineDraft: draft };
    const current = { name: 'Old lathe', serialNumber: 'S-1', description: 'Kept', rowVersion: 7 };
    const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [group] } }]);

    store.applyDraft('a1', 'md');
    await until(http, DRAFTS_URL, 'GET', { groups: [group] });
    const loaded = await untilRequest(http, MACHINE_DRAFT_URL, 'GET', { current });
    const applied = await until(http, MACHINE_APPLY_URL, 'POST', { machineId: 'm1' });

    expect(applied).toEqual({ name: 'Old lathe', serialNumber: 'S-2', description: 'Kept', rowVersion: 7 });
    expect(loaded.context.get(SKIP_ERROR_TOAST)).toBe(true);
  });

  it('applyDraft_ForAMachineUpdateDraftWithoutACurrentMachine_SendsNoApplyAndShowsTheError', async () => {
    const draft = { ...MACHINE_DRAFT, isCreate: false };
    const group = { ...EXISTING_GROUP, machineDraft: draft };
    const { store, http, messages } = await setup([{ attemptId: 'a1', drafts: { groups: [group] } }]);

    store.applyDraft('a1', 'md');
    await until(http, DRAFTS_URL, 'GET', { groups: [group] });
    await until(http, MACHINE_DRAFT_URL, 'GET', { current: null });
    await TestBed.inject(ApplicationRef).whenStable();

    http.expectNone(MACHINE_APPLY_URL);
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: false });
  });

  it('applyDraft_ForAMetricDraft_SendsOneApplyCallAndMarksTheDraft', async () => {
    const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [EXISTING_GROUP] } }]);

    store.applyDraft('a1', 'mt');
    await until(http, DRAFTS_URL, 'GET', { groups: [EXISTING_GROUP] });
    await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
    const applied = (await until(http, DEFINITIONS_URL, 'PUT', { revision: 4 })) as { created: unknown[] };
    const marked = await until(http, MARK_URL, 'POST');

    expect(applied.created).toHaveLength(1);
    expect(marked).toEqual({ ids: ['mt'] });
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
  });

  it('applyDraft_ForAMetricDraftEqualToTheSavedDefinition_SendsNoApplyCallButMarksAndCountsIt', async () => {
    const saved = {
      id: 'd1',
      key: 'spindle.temperature',
      name: 'Spindle temperature',
      description: null,
      valueKind: 'numeric',
      unit: { quantityKind: 'Temperature', name: 'Celsius', abbreviation: '°C' },
      min: null,
      max: null,
      states: null,
      createdAt: '',
      updatedAt: null,
    };
    const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [EXISTING_GROUP] } }]);
    const translate = vi.spyOn(TestBed.inject(TranslateService), 'instant');

    store.applyDraft('a1', 'mt');
    await until(http, DRAFTS_URL, 'GET', { groups: [EXISTING_GROUP] });
    await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [saved] });
    const marked = await until(http, MARK_URL, 'POST');
    await TestBed.inject(ApplicationRef).whenStable();

    http.expectNone((candidate) => candidate.url === DEFINITIONS_URL && candidate.method === 'PUT');
    expect(marked).toEqual({ ids: ['mt'] });
    expect(translate).toHaveBeenCalledWith('DRAFTS.QUICK.APPLIED', { count: 1, skipped: 0 });
  });

  it('applyGroup_WithStatusDraftsWithoutAValidSeverityOrDescription_SendsNullsNotDefaults', async () => {
    const statuses = [
      { ...STATUS_DRAFT, id: 'st1', code: ' E1 ', severity: null, description: null },
      { ...STATUS_DRAFT, id: 'st2', code: 'E2', severity: 'urgent', description: '<p>New</p>' },
    ];
    const group = { ...EXISTING_GROUP, metrics: [], statuses };
    const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [group] } }]);

    store.applyGroup('a1', { machineDraftId: null, machineId: 'm1' });
    await until(http, DRAFTS_URL, 'GET', { groups: [group] });
    const batch = await until(http, STATUSES_APPLY_URL, 'POST');
    await until(http, MARK_URL, 'POST');

    expect(batch).toEqual({
      items: [
        { code: 'E1', severity: null, description: null },
        { code: 'E2', severity: null, description: '<p>New</p>' },
      ],
    });
  });

  it('applyDraft_ForAStatusDraft_SendsTheBatchWithTheSkipToastContext', async () => {
    const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [EXISTING_GROUP] } }]);

    store.applyDraft('a1', 'st');
    await until(http, DRAFTS_URL, 'GET', { groups: [EXISTING_GROUP] });
    const batch = await untilRequest(http, STATUSES_APPLY_URL, 'POST');
    await until(http, MARK_URL, 'POST');

    expect(batch.context.get(SKIP_ERROR_TOAST)).toBe(true);
  });

  describe('applyMachine', () => {
    const SEARCH_URL = '/api/core/v1/machines/m1/drafts/search';
    const item = (attemptId: string): object => ({ id: `d-${attemptId}`, attemptId });
    const group = (metricId: string): object => ({
      ...EXISTING_GROUP,
      metrics: [{ ...METRIC_DRAFT, id: metricId }],
      statuses: [],
    });

    it('applyMachine_WithTwoPagesFromTwoAttempts_ReadsThePagesThenAppliesEachAttemptOldestFirst', async () => {
      const { store, http } = await setup([]);
      const order: string[] = [];

      store.applyMachine('m1');
      const first = (await until(http, SEARCH_URL, 'POST', { items: [item('a1')], totalCount: 101 })) as object;
      order.push('page-1');
      const second = (await until(http, SEARCH_URL, 'POST', { items: [item('a2')], totalCount: 101 })) as object;
      order.push('page-2');
      await until(http, '/api/core/v1/ingest-attempts/a1/drafts', 'GET', { groups: [group('m-a1')] });
      await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
      const firstApply = (await until(http, DEFINITIONS_URL, 'PUT', { revision: 4 })) as { expectedRevision: number };
      const firstMark = await until(http, MARK_URL, 'POST');
      order.push('attempt-a1');
      await until(http, '/api/core/v1/ingest-attempts/a2/drafts', 'GET', { groups: [group('m-a2')] });
      await until(http, DEFINITIONS_URL, 'GET', { revision: 4, definitions: [] });
      const secondApply = (await until(http, DEFINITIONS_URL, 'PUT', { revision: 5 })) as { expectedRevision: number };
      const secondMark = await until(http, MARK_URL, 'POST');
      order.push('attempt-a2');

      expect(first).toEqual({
        offset: 0,
        limit: 100,
        sort: { column: 'proposedAt', direction: 'asc' },
        filter: {},
      });
      expect(second).toEqual({ ...first, offset: 100 });
      expect(order).toEqual(['page-1', 'page-2', 'attempt-a1', 'attempt-a2']);
      expect([firstApply.expectedRevision, secondApply.expectedRevision]).toEqual([3, 4]);
      expect([firstMark, secondMark]).toEqual([{ ids: ['m-a1'] }, { ids: ['m-a2'] }]);
      expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
    });

    it('applyMachine_WhenTheFirstAttemptFails_SendsNothingForTheSecondAttempt', async () => {
      const { store, http, messages } = await setup([]);

      store.applyMachine('m1');
      await until(http, SEARCH_URL, 'POST', { items: [item('a1'), item('a2')], totalCount: 2 });
      await until(http, '/api/core/v1/ingest-attempts/a1/drafts', 'GET', { groups: [group('m-a1')] });
      await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
      await until(http, DEFINITIONS_URL, 'PUT', new Error('conflict'));
      await TestBed.inject(ApplicationRef).whenStable();

      http.expectNone('/api/core/v1/ingest-attempts/a2/drafts');
      expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
      expect(store.quickRun()).toEqual({ count: 1, hasApplied: false });
    });
  });
});
