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
import { RealtimeTopic } from '@shared/realtime/realtime';
import {
  createFakeRealtimeConnection,
  provideFakeRealtimeConnection,
} from '@shared/realtime/testing/fake-realtime-connection';

const DRAFTS_URL = '/api/core/v1/ingest-attempts/a1/drafts';
const MACHINE_APPLY_URL = '/api/core/v1/machine-drafts/md/apply';
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
  warningLow: null,
  warningHigh: null,
  alarmLow: null,
  alarmHigh: null,
  behavior: null,
};
const STATUS_DRAFT = {
  id: 'st',
  state: DraftState.Pending,
  sourceLocation: null,
  code: 'E12',
  severity: 'warning',
  description: null,
};

const PHASE_APPLY_URL = '/api/telemetry/v1/machines/m1/definition-drafts/operating-phases/ph/apply';
const PHASE_CONDITION = {
  metricDefinitionId: 'def',
  metricDraftId: null,
  statusDefinitionId: null,
  statusDraftId: null,
  operator: 'greaterThan',
  value: 5,
  upperValue: null,
  states: null,
};
const PHASE_DRAFT = {
  id: 'ph',
  state: DraftState.Pending,
  sourceLocation: null,
  code: 'cutting',
  name: 'Cutting',
  description: null,
  priority: 1,
  isTracked: true,
  conditions: [PHASE_CONDITION],
  limits: [],
};
const PHASE_WAITING = {
  ...PHASE_DRAFT,
  conditions: [{ ...PHASE_CONDITION, metricDefinitionId: null, metricDraftId: 'mt' }],
};

const CREATE_GROUP = {
  machineId: null,
  machineName: null,
  machineDraft: MACHINE_DRAFT,
  metrics: [METRIC_DRAFT],
  statuses: [STATUS_DRAFT],
  phases: [],
};
const MOVED_GROUP = {
  machineId: 'm1',
  machineName: 'Lathe',
  machineDraft: { ...MACHINE_DRAFT, state: DraftState.Applied },
  metrics: [METRIC_DRAFT],
  statuses: [STATUS_DRAFT],
  phases: [],
};
const EXISTING_GROUP = { ...MOVED_GROUP, machineDraft: null };

const HostStore = signalStore(withDraftQuickApply());
const realtime = createFakeRealtimeConnection();

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
      request.flush({ detail: body.message }, { status: errorStatus, statusText: 'Error', headers: errorHeaders });
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
      provideFakeRealtimeConnection(realtime),
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

describe('withDraftQuickApply', { tags: ['Drafts'] }, () => {
  it('applyDraft_ForAPhaseDraftThatNamesOnlyDefinitions_SendsTheApplyToTelemetryWithTheDraftValues', async () => {
    const group = { ...EXISTING_GROUP, metrics: [], statuses: [], phases: [PHASE_DRAFT] };
    const { store, http, messages } = await setup([{ attemptId: 'a1', drafts: { groups: [group] } }]);

    store.applyDraft('a1', 'ph');
    await until(http, DRAFTS_URL, 'GET', { groups: [group] });
    const body = await until(http, PHASE_APPLY_URL, 'POST', { id: 'p1' });
    await TestBed.inject(ApplicationRef).whenStable();

    http.verify();
    expect(body).toEqual(
      expect.objectContaining({ code: 'cutting', name: 'Cutting', priority: 1, isTracked: true, limits: [] }),
    );
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });

  it('applyGroup_WithAPhaseThatNamesAMetricDraft_AppliesThePhaseLastAfterAFreshRead', async () => {
    const waiting = { ...EXISTING_GROUP, statuses: [], phases: [PHASE_WAITING] };
    const moved = { ...EXISTING_GROUP, metrics: [], statuses: [], phases: [PHASE_DRAFT] };
    const { store, http, messages } = await setup([{ attemptId: 'a1', drafts: { groups: [waiting] } }]);

    store.applyGroup('a1', { machineDraftId: null, machineId: 'm1' });
    await until(http, DRAFTS_URL, 'GET', { groups: [waiting] });
    await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
    await until(http, DEFINITIONS_URL, 'PUT', { revision: 4 });
    await until(http, DRAFTS_URL, 'GET', { groups: [moved] });
    const body = await until(http, PHASE_APPLY_URL, 'POST', { id: 'p1' });
    await TestBed.inject(ApplicationRef).whenStable();

    http.verify();
    expect(body).toEqual(expect.objectContaining({ code: 'cutting' }));
    expect(messages.add).toHaveBeenCalledWith(
      expect.objectContaining({ severity: 'success', detail: 'DRAFTS.QUICK.APPLIED' }),
    );
  });

  it('applyGroup_WhenThePhaseStillNamesAPendingDraftAfterTheFreshRead_SkipsItAndReportsTheCount', async () => {
    const waiting = { ...EXISTING_GROUP, statuses: [], phases: [PHASE_WAITING] };
    const { store, http, messages } = await setup([{ attemptId: 'a1', drafts: { groups: [waiting] } }]);

    store.applyGroup('a1', { machineDraftId: null, machineId: 'm1' });
    await until(http, DRAFTS_URL, 'GET', { groups: [waiting] });
    await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
    await until(http, DEFINITIONS_URL, 'PUT', { revision: 4 });
    await until(http, DRAFTS_URL, 'GET', { groups: [waiting] });
    await TestBed.inject(ApplicationRef).whenStable();

    http.verify();
    expect(messages.add).toHaveBeenCalledWith(
      expect.objectContaining({ severity: 'success', detail: 'DRAFTS.QUICK.APPLIED_SKIPPED' }),
    );
  });

  it('applyDraft_ForAStatusDraft_SendsABatchOfOneItemWithTheDraftId', async () => {
    const { store, http, messages } = await setup([{ attemptId: 'a1', drafts: { groups: [EXISTING_GROUP] } }]);

    store.applyDraft('a1', 'st');
    await until(http, DRAFTS_URL, 'GET', { groups: [EXISTING_GROUP] });
    const batch = await until(http, STATUSES_APPLY_URL, 'POST');

    expect(batch).toEqual({ items: [{ code: 'E12', severity: 'warning', description: null, draftId: 'st' }] });
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
    expect(messages.add).toHaveBeenCalledWith(
      expect.objectContaining({ severity: 'success', detail: 'DRAFTS.QUICK.APPLIED' }),
    );
    expect(store.applyingIds()).toEqual([]);
  });

  it('applyGroup_ForACreateGroup_AppliesTheMachineThenOneMetricCallThenOneStatusBatchWithTheDraftIds', async () => {
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
      created: { draftId: string }[];
      expectedRevision: number;
    };
    order.push('metrics');
    const statusBody = (await until(http, STATUSES_APPLY_URL, 'POST')) as { items: { draftId: string }[] };
    order.push('statuses');
    await TestBed.inject(ApplicationRef).whenStable();

    http.verify();
    expect(order).toEqual(['machine', 'reload', 'metrics', 'statuses']);
    expect(metricBody.created).toHaveLength(1);
    expect(metricBody.expectedRevision).toBe(3);
    expect(metricBody.created[0].draftId).toBe('mt');
    expect(statusBody.items[0].draftId).toBe('st');
    expect(messages.add).toHaveBeenCalledWith(
      expect.objectContaining({ severity: 'success', detail: 'DRAFTS.QUICK.APPLIED' }),
    );
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
  });

  it('applyDrafts_WithTwoStatusDrafts_AppliesThemOneAfterTheOtherInOneRun', async () => {
    const second = { ...STATUS_DRAFT, id: 'st2', code: 'E13' };
    const group = { ...EXISTING_GROUP, metrics: [], statuses: [STATUS_DRAFT, second] };
    const { store, http, messages } = await setup([{ attemptId: 'a1', drafts: { groups: [group] } }]);

    store.applyDrafts([
      { attemptId: 'a1', draftId: 'st' },
      { attemptId: 'a1', draftId: 'st2' },
    ]);
    await until(http, DRAFTS_URL, 'GET', { groups: [group] });
    const firstBody = (await until(http, STATUSES_APPLY_URL, 'POST')) as { items: { draftId: string }[] };
    await until(http, DRAFTS_URL, 'GET', { groups: [group] });
    const secondBody = (await until(http, STATUSES_APPLY_URL, 'POST')) as { items: { draftId: string }[] };
    await TestBed.inject(ApplicationRef).whenStable();

    http.verify();
    expect(firstBody.items[0].draftId).toBe('st');
    expect(secondBody.items[0].draftId).toBe('st2');
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
    expect(messages.add).toHaveBeenCalledTimes(1);
  });

  it('applyAttempt_WithTwoGroups_AppliesTheSecondGroupAfterTheFirstInOneRun', async () => {
    const first = { ...EXISTING_GROUP, metrics: [] };
    const second = { ...first, machineId: 'm2', statuses: [{ ...STATUS_DRAFT, id: 'st2' }] };
    const drafts = { groups: [first, second] };
    const { store, http, messages } = await setup([{ attemptId: 'a1', drafts }]);

    store.applyAttempt('a1');
    await until(http, DRAFTS_URL, 'GET', drafts);
    const firstBody = (await until(http, STATUSES_APPLY_URL, 'POST')) as { items: { draftId: string }[] };
    await until(http, DRAFTS_URL, 'GET', drafts);
    const secondBody = (await until(http, '/api/telemetry/v1/machines/m2/status-definitions/apply', 'POST')) as {
      items: { draftId: string }[];
    };
    await TestBed.inject(ApplicationRef).whenStable();

    http.verify();
    expect(firstBody.items[0].draftId).toBe('st');
    expect(secondBody.items[0].draftId).toBe('st2');
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
    expect(messages.add).toHaveBeenCalledTimes(1);
  });

  it('applyGroup_WithThirtyStatusesAndTwentyMetrics_SendsOneMetricApplyAndOneStatusBatchWithEveryDraftId', async () => {
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
    const metricBody = (await until(http, DEFINITIONS_URL, 'PUT', { revision: 4 })) as {
      created: { draftId: string }[];
    };
    const batch = (await until(http, STATUSES_APPLY_URL, 'POST')) as { items: { draftId: string }[] };
    await TestBed.inject(ApplicationRef).whenStable();

    http.verify();
    expect(metricBody.created).toHaveLength(20);
    expect(batch.items).toHaveLength(30);
    expect(metricBody.created.map((item) => item.draftId)).toEqual(metrics.map((draft) => draft.id));
    expect(batch.items.map((item) => item.draftId)).toEqual(statuses.map((draft) => draft.id));
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
  });

  it('applyGroup_WhenTheStatusBatchFails_ReportsTheFailureAfterTheMetricsWereSaved', async () => {
    const { store, http, messages } = await setup([{ attemptId: 'a1', drafts: { groups: [EXISTING_GROUP] } }]);

    store.applyGroup('a1', { machineDraftId: null, machineId: 'm1' });
    await until(http, DRAFTS_URL, 'GET', { groups: [EXISTING_GROUP] });
    await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
    await until(http, DEFINITIONS_URL, 'PUT', { revision: 4 });
    await until(http, STATUSES_APPLY_URL, 'POST', new Error('conflict'));
    await TestBed.inject(ApplicationRef).whenStable();

    expect(messages.add).toHaveBeenCalledWith(
      expect.objectContaining({ severity: 'error', detail: 'DRAFTS.QUICK.FAILED' }),
    );
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
  });

  it('applyGroup_ForACreateGroupWhenTelemetryDoesNotKnowTheMachineYet_AsksAgainAndThenApplies', async () => {
    const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [CREATE_GROUP] } }]);
    store.applyGroup('a1', { machineDraftId: 'md', machineId: null });
    await until(http, DRAFTS_URL, 'GET', { groups: [CREATE_GROUP] });
    await until(http, MACHINE_APPLY_URL, 'POST', { machineId: 'm1' });
    await until(http, DRAFTS_URL, 'GET', { groups: [MOVED_GROUP] });
    await until(http, DEFINITIONS_URL, 'GET', new Error('not known yet'), HttpStatusCode.NotFound);
    await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
    await until(http, DEFINITIONS_URL, 'PUT', { revision: 4 });
    await until(http, STATUSES_APPLY_URL, 'POST');
    await TestBed.inject(ApplicationRef).whenStable();

    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
  });

  it('applyDraft_ForAStatusWhenTelemetryDoesNotKnowTheMachineYet_AsksAgainAndThenApplies', async () => {
    const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [EXISTING_GROUP] } }]);

    store.applyDraft('a1', 'st');
    await until(http, DRAFTS_URL, 'GET', { groups: [EXISTING_GROUP] });
    await until(http, STATUSES_APPLY_URL, 'POST', new Error('not known yet'), HttpStatusCode.NotFound);
    await until(http, STATUSES_APPLY_URL, 'POST');

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

      expect(translate).toHaveBeenCalledWith('DRAFTS.QUICK.MACHINE_NOT_READY');
      expect(messages.add).toHaveBeenCalledWith(
        expect.objectContaining({ severity: 'error', detail: 'DRAFTS.QUICK.FAILED' }),
      );
      expect(store.quickRun()).toEqual({ count: 1, hasApplied: false });
    } finally {
      vi.useRealTimers();
    }
  });

  it('applyDraft_WhenAHintOfTheMachineArrivesDuringTheWait_RunsTheNextTryAtOnce', async () => {
    const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [EXISTING_GROUP] } }]);
    vi.useFakeTimers();
    try {
      store.applyDraft('a1', 'st');
      await vi.advanceTimersByTimeAsync(0);
      http.expectOne(DRAFTS_URL).flush({ groups: [EXISTING_GROUP] });
      await vi.advanceTimersByTimeAsync(0);
      http
        .expectOne((candidate) => candidate.url === STATUSES_APPLY_URL)
        .flush(null, { status: HttpStatusCode.NotFound, statusText: 'Not Found' });
      await vi.advanceTimersByTimeAsync(100);
      http.expectNone(STATUSES_APPLY_URL);

      realtime.hints$.next({ topic: RealtimeTopic.DefinitionDrafts, machineId: 'm1' });
      await vi.advanceTimersByTimeAsync(0);
      http.expectOne((candidate) => candidate.url === STATUSES_APPLY_URL).flush(null);
      await vi.advanceTimersByTimeAsync(0);

      expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
    } finally {
      vi.useRealTimers();
    }
  });

  it.each([
    { topic: RealtimeTopic.DefinitionDrafts, machineId: 'other-machine' },
    { topic: RealtimeTopic.DefinitionDrafts, attemptId: 'other-attempt' },
    { topic: RealtimeTopic.Machines, machineId: 'm1' },
  ])('applyDraft_WhenAnotherHintArrivesDuringTheWait_KeepsWaiting', async (hint) => {
    const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [EXISTING_GROUP] } }]);
    vi.useFakeTimers();
    try {
      store.applyDraft('a1', 'st');
      await vi.advanceTimersByTimeAsync(0);
      http.expectOne(DRAFTS_URL).flush({ groups: [EXISTING_GROUP] });
      await vi.advanceTimersByTimeAsync(0);
      http
        .expectOne((candidate) => candidate.url === STATUSES_APPLY_URL)
        .flush(null, { status: HttpStatusCode.NotFound, statusText: 'Not Found' });

      realtime.hints$.next(hint);
      await vi.advanceTimersByTimeAsync(499);

      http.expectNone(STATUSES_APPLY_URL);
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
      expect(translate).toHaveBeenCalledWith('DRAFTS.QUICK.RATE_LIMITED');
      expect(messages.add).toHaveBeenCalledWith(
        expect.objectContaining({ severity: 'error', detail: 'DRAFTS.QUICK.FAILED' }),
      );
      expect(store.quickRun()).toEqual({ count: 1, hasApplied: false });
    } finally {
      vi.useRealTimers();
    }
  });

  it('applyGroup_WhenTheMetricApplyConflicts_AppliesTheMetricsOneByOneSkipsTheConflictAndStillSendsTheStatuses', async () => {
    const second = { ...METRIC_DRAFT, id: 'mt2', key: 'spindle.load', name: 'Spindle load' };
    const group = { ...EXISTING_GROUP, metrics: [METRIC_DRAFT, second] };
    const { store, http, messages } = await setup([{ attemptId: 'a1', drafts: { groups: [group] } }]);

    store.applyGroup('a1', { machineDraftId: null, machineId: 'm1' });
    await until(http, DRAFTS_URL, 'GET', { groups: [group] });
    await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
    await until(http, DEFINITIONS_URL, 'PUT', new Error('conflict'));
    await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
    await until(http, DEFINITIONS_URL, 'PUT', new Error('conflict'));
    await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
    const saved = (await until(http, DEFINITIONS_URL, 'PUT', { revision: 4 })) as { created: { draftId: string }[] };
    const statuses = (await until(http, STATUSES_APPLY_URL, 'POST')) as { items: { draftId: string }[] };
    await TestBed.inject(ApplicationRef).whenStable();

    http.verify();
    expect(saved.created.map((item) => item.draftId)).toEqual(['mt2']);
    expect(statuses.items.map((item) => item.draftId)).toEqual(['st']);
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
    expect(messages.add).toHaveBeenCalledWith(
      expect.objectContaining({ severity: 'success', detail: 'DRAFTS.QUICK.APPLIED_SKIPPED' }),
    );
  });

  it('applyGroup_ForACreateGroupWhenTheChildDraftsStillAwaitTheirTarget_ReadsAgainAndThenAppliesThem', async () => {
    const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [CREATE_GROUP] } }]);
    const awaiting = {
      ...MOVED_GROUP,
      metrics: [{ ...METRIC_DRAFT, state: DraftState.AwaitingTarget }],
      statuses: [{ ...STATUS_DRAFT, state: DraftState.AwaitingTarget }],
    };

    store.applyGroup('a1', { machineDraftId: 'md', machineId: null });
    await until(http, DRAFTS_URL, 'GET', { groups: [CREATE_GROUP] });
    await until(http, MACHINE_APPLY_URL, 'POST', { machineId: 'm1' });
    await until(http, DRAFTS_URL, 'GET', { groups: [awaiting] });
    await until(http, DRAFTS_URL, 'GET', { groups: [MOVED_GROUP] });
    await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
    const metrics = (await until(http, DEFINITIONS_URL, 'PUT', { revision: 4 })) as { created: { draftId: string }[] };
    const statuses = (await until(http, STATUSES_APPLY_URL, 'POST')) as { items: { draftId: string }[] };
    await TestBed.inject(ApplicationRef).whenStable();

    expect(metrics.created.map((item) => item.draftId)).toEqual(['mt']);
    expect(statuses.items.map((item) => item.draftId)).toEqual(['st']);
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
  });

  it('applyGroup_ForACreateGroupWhenTheFirstReadHoldsOnlyTheMachineDraft_ReadsAgainUntilTheChildrenMoved', async () => {
    const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [CREATE_GROUP] } }]);
    const machineOnly = { ...MOVED_GROUP, metrics: [], statuses: [] };

    store.applyGroup('a1', { machineDraftId: 'md', machineId: null });
    await until(http, DRAFTS_URL, 'GET', { groups: [CREATE_GROUP] });
    await until(http, MACHINE_APPLY_URL, 'POST', { machineId: 'm1' });
    await until(http, DRAFTS_URL, 'GET', { groups: [machineOnly] });
    await until(http, DRAFTS_URL, 'GET', { groups: [MOVED_GROUP] });
    await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
    const metrics = (await until(http, DEFINITIONS_URL, 'PUT', { revision: 4 })) as { created: { draftId: string }[] };
    const statuses = (await until(http, STATUSES_APPLY_URL, 'POST')) as { items: { draftId: string }[] };
    await TestBed.inject(ApplicationRef).whenStable();

    expect(metrics.created.map((item) => item.draftId)).toEqual(['mt']);
    expect(statuses.items.map((item) => item.draftId)).toEqual(['st']);
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
  });

  it('applyAttempt_WhenALaterGroupIsGoneAfterTheFirstGroupMovedItsDrafts_EndsWithoutAnError', async () => {
    const orphan = { ...CREATE_GROUP, machineDraft: null };
    const drafts = { groups: [CREATE_GROUP, orphan] };
    const { store, http, messages } = await setup([{ attemptId: 'a1', drafts }]);

    store.applyAttempt('a1');
    await until(http, DRAFTS_URL, 'GET', drafts);
    await until(http, MACHINE_APPLY_URL, 'POST', { machineId: 'm1' });
    await until(http, DRAFTS_URL, 'GET', { groups: [MOVED_GROUP] });
    await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
    await until(http, DEFINITIONS_URL, 'PUT', { revision: 4 });
    await until(http, STATUSES_APPLY_URL, 'POST');
    await until(http, DRAFTS_URL, 'GET', { groups: [MOVED_GROUP] });
    await TestBed.inject(ApplicationRef).whenStable();

    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
    expect(messages.add).not.toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });

  it('applyGroup_WhenTheMetricApplyAnswers422DraftNotPending_ShowsTheDraftNotPendingReason', async () => {
    const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [EXISTING_GROUP] } }]);
    const translate = vi.spyOn(TestBed.inject(TranslateService), 'instant');

    store.applyGroup('a1', { machineDraftId: null, machineId: 'm1' });
    await until(http, DRAFTS_URL, 'GET', { groups: [EXISTING_GROUP] });
    await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
    await until(http, DEFINITIONS_URL, 'PUT', new Error('draftNotPending'), HttpStatusCode.UnprocessableEntity);
    await TestBed.inject(ApplicationRef).whenStable();

    expect(translate).toHaveBeenCalledWith('DRAFTS.CONFLICTS.DRAFT_NOT_PENDING');
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: false });
  });

  it('applyGroup_WithAMetricThatNeedsReview_SkipsItAndReportsTheCount', async () => {
    const badMetric = { ...METRIC_DRAFT, unitName: 'Parsec' };
    const group = { ...EXISTING_GROUP, metrics: [badMetric] };
    const { store, http, messages } = await setup([{ attemptId: 'a1', drafts: { groups: [group] } }]);

    store.applyGroup('a1', { machineDraftId: null, machineId: 'm1' });
    await until(http, DRAFTS_URL, 'GET', { groups: [group] });
    await until(http, STATUSES_APPLY_URL, 'POST');
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

  it('applyDraft_ForAMetricDraft_SendsOneApplyCallWithTheDraftId', async () => {
    const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [EXISTING_GROUP] } }]);

    store.applyDraft('a1', 'mt');
    await until(http, DRAFTS_URL, 'GET', { groups: [EXISTING_GROUP] });
    await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
    const applied = (await until(http, DEFINITIONS_URL, 'PUT', { revision: 4 })) as { created: { draftId: string }[] };

    expect(applied.created).toHaveLength(1);
    expect(applied.created[0].draftId).toBe('mt');
    expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
  });

  it('applyDraft_ForAMetricDraftEqualToTheSavedDefinition_SendsTheDraftAsAnUpdateSoTelemetryMarksItApplied', async () => {
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
    const applied = (await until(http, DEFINITIONS_URL, 'PUT', { revision: 4 })) as {
      created: unknown[];
      updated: { id: string; draftId: string }[];
    };
    await TestBed.inject(ApplicationRef).whenStable();

    expect(applied.created).toEqual([]);
    expect(applied.updated.map((item) => [item.id, item.draftId])).toEqual([['d1', 'mt']]);
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

    expect(batch).toEqual({
      items: [
        { code: 'E1', severity: null, description: null, draftId: 'st1' },
        { code: 'E2', severity: null, description: '<p>New</p>', draftId: 'st2' },
      ],
    });
  });

  it('applyDraft_ForAStatusDraft_SendsTheBatchWithTheSkipToastContext', async () => {
    const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [EXISTING_GROUP] } }]);

    store.applyDraft('a1', 'st');
    await until(http, DRAFTS_URL, 'GET', { groups: [EXISTING_GROUP] });
    const batch = await untilRequest(http, STATUSES_APPLY_URL, 'POST');

    expect(batch.context.get(SKIP_ERROR_TOAST)).toBe(true);
  });

  describe('isQuickBusy', () => {
    it('applyMachine_WithNoCachedDrafts_IsBusyUntilTheRunEnds', async () => {
      const { store, http } = await setup([]);

      store.applyMachine('m1');
      const busyWhileRunning = store.isQuickBusy();
      await until(http, '/api/core/v1/machines/m1/drafts/search', 'POST', {
        items: [],
        totalCount: 0,
        nextPosition: null,
      });
      await TestBed.inject(ApplicationRef).whenStable();

      expect(busyWhileRunning).toBe(true);
      expect(store.isQuickBusy()).toBe(false);
    });

    it('applyAttempt_WhenTheCachedGroupHasNoPendingDraft_IsBusyUntilTheRunEnds', async () => {
      const applied = { ...EXISTING_GROUP, metrics: [{ ...METRIC_DRAFT, state: DraftState.Applied }], statuses: [] };
      const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [applied] } }]);

      store.applyAttempt('a1');
      const busyWhileRunning = store.isQuickBusy();
      await until(http, DRAFTS_URL, 'GET', { groups: [] });
      await TestBed.inject(ApplicationRef).whenStable();

      expect(busyWhileRunning).toBe(true);
      expect(store.isQuickBusy()).toBe(false);
    });

    it('applyDraft_WhenTheRunFails_IsNotBusyAfterTheFailure', async () => {
      const { store, http } = await setup([{ attemptId: 'a1', drafts: { groups: [EXISTING_GROUP] } }]);

      store.applyDraft('a1', 'st');
      const busyWhileRunning = store.isQuickBusy();
      await until(http, DRAFTS_URL, 'GET', new Error('server error'), HttpStatusCode.InternalServerError);
      await TestBed.inject(ApplicationRef).whenStable();

      expect(busyWhileRunning).toBe(true);
      expect(store.isQuickBusy()).toBe(false);
    });
  });
  describe('applyMachine', () => {
    const SEARCH_URL = '/api/core/v1/machines/m1/drafts/search';
    const NEXT_POSITION = { sortValue: 'd-a1', id: 'a1' };
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
      const first = (await until(http, SEARCH_URL, 'POST', {
        items: [item('a1')],
        totalCount: 101,
        nextPosition: NEXT_POSITION,
      })) as object;
      order.push('page-1');
      const second = (await until(http, SEARCH_URL, 'POST', {
        items: [item('a2')],
        totalCount: 101,
        nextPosition: null,
      })) as object;
      order.push('page-2');
      await until(http, '/api/core/v1/ingest-attempts/a1/drafts', 'GET', { groups: [group('m-a1')] });
      await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
      const firstApply = (await until(http, DEFINITIONS_URL, 'PUT', { revision: 4 })) as { expectedRevision: number };
      order.push('attempt-a1');
      await until(http, '/api/core/v1/ingest-attempts/a2/drafts', 'GET', { groups: [group('m-a2')] });
      await until(http, DEFINITIONS_URL, 'GET', { revision: 4, definitions: [] });
      const secondApply = (await until(http, DEFINITIONS_URL, 'PUT', { revision: 5 })) as { expectedRevision: number };
      order.push('attempt-a2');

      expect(first).toEqual({
        limit: 100,
        sort: { column: 'proposedAt', direction: 'asc' },
        filter: { states: { values: ['pending'] } },
      });
      expect(second).toEqual({ ...first, after: NEXT_POSITION });
      expect(order).toEqual(['page-1', 'page-2', 'attempt-a1', 'attempt-a2']);
      expect([firstApply.expectedRevision, secondApply.expectedRevision]).toEqual([3, 4]);
      expect(store.quickRun()).toEqual({ count: 1, hasApplied: true });
    });

    it('applyMachine_WhenTheFirstAttemptFails_SendsNothingForTheSecondAttempt', async () => {
      const { store, http, messages } = await setup([]);

      store.applyMachine('m1');
      await until(http, SEARCH_URL, 'POST', {
        items: [item('a1'), item('a2')],
        totalCount: 2,
        nextPosition: null,
      });
      await until(http, '/api/core/v1/ingest-attempts/a1/drafts', 'GET', { groups: [group('m-a1')] });
      await until(http, DEFINITIONS_URL, 'GET', { revision: 3, definitions: [] });
      await until(http, DEFINITIONS_URL, 'PUT', new Error('server error'), HttpStatusCode.InternalServerError);
      await TestBed.inject(ApplicationRef).whenStable();

      http.expectNone('/api/core/v1/ingest-attempts/a2/drafts');
      expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
      expect(store.quickRun()).toEqual({ count: 1, hasApplied: false });
    });
  });
});
