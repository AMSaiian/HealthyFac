import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withAutoCleanupInjectors, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';

import { CompanyStore } from '@shared/company/company.store';
import { AuthStore } from '@shared/security/auth-store/auth.store';

import { MachineState } from '@machines/machine';
import { MACHINES_ROUTES } from '@machines/machines.routes';
import { MetricValueKind } from '@machines/metrics/metric-definition';
import { MetricChartPage } from '@machines/signals/metric-chart-page/metric-chart-page';
import { MetricChartPageStore } from '@machines/signals/metric-chart-page-store/metric-chart-page.store';
import { ChartRange } from '@machines/signals/signal-episode';

vi.mock('chartjs-plugin-zoom', () => ({ default: { id: 'zoom' } }));
vi.mock('chart.js/auto', () => ({
  default: class {
    static register = vi.fn();
    update = vi.fn();
    destroy = vi.fn();
  },
}));

const MACHINE_URL = '/api/core/v1/machines/m1';
const DEFINITIONS_URL = '/api/telemetry/v1/machines/m1/metric-definitions';
const HISTORY_URL = '/api/telemetry/v1/machines/m1/metric-history';
const EPISODES_URL = '/api/telemetry/v1/machines/m1/signal-episodes';
const PHASE_TIMES_URL = '/api/telemetry/v1/machines/m1/phase-times';
const STATE_TIMES_URL = '/api/core/v1/machines/m1/state-times';
const NO_PHASE_TIMES = { phases: [], unknownModeSeconds: 0, noDataSeconds: 3600 };
const NO_STATE_TIMES = { states: [], beforeCreationSeconds: 0 };
const DAY_MS = 24 * 60 * 60 * 1000;

const MACHINE = {
  id: 'm1',
  name: 'Lathe',
  serialNumber: 'S1',
  gatewayId: 'abcdefghijklmnop',
  state: MachineState.Operational,
  description: null,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: null,
};

function definition(id: string, valueKind: MetricValueKind): object {
  return {
    id,
    key: id,
    name: id,
    description: null,
    valueKind,
    unit: { quantityKind: 'q', name: 'n', abbreviation: 'u' },
    min: null,
    max: null,
    states: null,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: null,
  };
}

function episode(id: string, metricDefinitionId: string | null): object {
  return {
    episodeId: id,
    rule: 'threshold',
    severity: 'alarm',
    state: 'open',
    metricDefinitionId,
    metricKey: null,
    statusDefinitionId: null,
    statusCode: null,
    phaseCode: null,
    openedAt: '2026-01-01T10:00:00Z',
    resolvedAt: null,
  };
}

@Component({ template: '', selector: 'hf-not-found-stub', host: { class: 'contents' } })
class NotFoundStub {}

class ResizeObserverStub {
  observe = vi.fn();
  unobserve = vi.fn();
  disconnect = vi.fn();
}

function setup(): HttpTestingController {
  TestBed.configureTestingModule({
    providers: [
      provideRouter(
        [
          { path: 'machines', children: MACHINES_ROUTES },
          { path: 'not-found', component: NotFoundStub },
        ],
        withComponentInputBinding(),
        withAutoCleanupInjectors(),
      ),
      provideHttpClient(),
      provideHttpClientTesting(),
      provideTranslateService(),
      { provide: AuthStore, useValue: { hasPermission: () => true } },
      { provide: CompanyStore, useValue: { timeZoneId: () => 'Europe/Kyiv' } },
      { provide: MessageService, useValue: { add: vi.fn() } },
    ],
  });
  return TestBed.inject(HttpTestingController);
}

async function flushWhenRequested(http: HttpTestingController, url: string, body: object): Promise<void> {
  await vi.waitFor(() => {
    http.expectOne((request) => request.url === url).flush(body);
  });
}

async function openSignals(http: HttpTestingController): Promise<InstanceType<typeof MetricChartPageStore>> {
  const harness = await RouterTestingHarness.create();
  const navigation = harness.navigateByUrl('/machines/m1/signals');
  await flushWhenRequested(http, MACHINE_URL, MACHINE);
  await navigation;
  TestBed.tick();
  await flushWhenRequested(http, DEFINITIONS_URL, {
    revision: 1,
    definitions: [definition('flag', MetricValueKind.Boolean), definition('temp', MetricValueKind.Numeric)],
  });
  await flushWhenRequested(http, EPISODES_URL, {
    items: [episode('e1', 'temp'), episode('e2', 'flag'), episode('e3', null)],
  });
  await flushWhenRequested(http, PHASE_TIMES_URL, NO_PHASE_TIMES);
  await flushWhenRequested(http, STATE_TIMES_URL, NO_STATE_TIMES);
  await TestBed.inject(ApplicationRef).whenStable();
  expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(MetricChartPage);
  return harness.routeDebugElement?.injector.get(MetricChartPageStore) as InstanceType<typeof MetricChartPageStore>;
}

describe('signals route', () => {
  beforeEach(() => {
    vi.stubGlobal('ResizeObserver', ResizeObserverStub);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('signalsRoute_AfterOpening_SelectsTheFirstNumericMetricAndKeepsItsEpisodesOnly', async () => {
    const http = setup();

    const store = await openSignals(http);

    expect(store.selectedMetric()?.id).toBe('temp');
    expect(store.metricEpisodes().map((item) => item.episodeId)).toEqual(['e1']);
  });

  it('signalsRoute_BeforeTheWidthIsKnown_SendsNoHistoryRequest', async () => {
    const http = setup();

    await openSignals(http);

    http.expectNone(HISTORY_URL);
  });

  it('signalsRoute_WidthOf320Pixels_AsksForTheHistoryWith320Buckets', async () => {
    const http = setup();
    const store = await openSignals(http);

    store.setWidth(320);

    await vi.waitFor(() => {
      const request = http.expectOne((candidate) => candidate.url === HISTORY_URL);
      expect(request.request.params.get('bucketCount')).toBe('320');
      expect(request.request.params.get('metricDefinitionId')).toBe('temp');
      request.flush({ buckets: [] });
    });
  });

  it('signalsRoute_WidthAbove500Pixels_AsksForAtMost500Buckets', async () => {
    const http = setup();
    const store = await openSignals(http);

    store.setWidth(1800);

    await vi.waitFor(() => {
      const request = http.expectOne((candidate) => candidate.url === HISTORY_URL);
      expect(request.request.params.get('bucketCount')).toBe('500');
      request.flush({ buckets: [] });
    });
  });

  it('signalsRoute_ChangeOfTheRange_AsksForTheEpisodesAndTheHistoryOfTheNewRange', async () => {
    const http = setup();
    const store = await openSignals(http);
    store.setWidth(100);
    await flushWhenRequested(http, HISTORY_URL, { buckets: [] });

    store.selectRange(ChartRange.Day);

    await vi.waitFor(() => {
      const history = http.expectOne((candidate) => candidate.url === HISTORY_URL);
      const from = Date.parse(history.request.params.get('from') ?? '');
      const to = Date.parse(history.request.params.get('to') ?? '');
      expect(to - from).toBe(DAY_MS);
      history.flush({ buckets: [] });
    });
    await flushWhenRequested(http, EPISODES_URL, { items: [] });
  });

  it('signalsRoute_CustomRange_AsksForTheHistoryWithItsUtcFromTo', async () => {
    const http = setup();
    const store = await openSignals(http);
    store.setWidth(100);
    await flushWhenRequested(http, HISTORY_URL, { buckets: [] });

    store.selectCustomRange('2026-01-15T08:00:00.000Z', '2026-01-15T10:30:00.000Z');

    await vi.waitFor(() => {
      const history = http.expectOne((candidate) => candidate.url === HISTORY_URL);
      expect(history.request.params.get('from')).toBe('2026-01-15T08:00:00.000Z');
      expect(history.request.params.get('to')).toBe('2026-01-15T10:30:00.000Z');
      history.flush({ buckets: [] });
    });
    await flushWhenRequested(http, EPISODES_URL, { items: [] });
  });

  it('signalsRoute_Zoom_ReloadsOnlyAfterThePauseAndKeepsTheRangeBeforeTheZoom', async () => {
    const http = setup();
    const store = await openSignals(http);
    store.setWidth(100);
    await flushWhenRequested(http, HISTORY_URL, { buckets: [] });

    store.zoomTo('2026-01-15T08:00:00.000Z', '2026-01-15T10:00:00.000Z');
    await TestBed.inject(ApplicationRef).whenStable();
    http.expectNone(HISTORY_URL);

    await vi.waitFor(() => {
      const history = http.expectOne((candidate) => candidate.url === HISTORY_URL);
      expect(history.request.params.get('from')).toBe('2026-01-15T08:00:00.000Z');
      history.flush({ buckets: [] });
    });
    await flushWhenRequested(http, EPISODES_URL, { items: [] });
    expect(store.zoomBase()).toEqual({ kind: 'preset', preset: ChartRange.Hour });
  });

  it('signalsRoute_ResetZoom_ReturnsToTheRangeBeforeTheZoom', async () => {
    const http = setup();
    const store = await openSignals(http);
    store.setWidth(100);
    await flushWhenRequested(http, HISTORY_URL, { buckets: [] });
    store.zoomTo('2026-01-15T08:00:00.000Z', '2026-01-15T10:00:00.000Z');
    await flushWhenRequested(http, HISTORY_URL, { buckets: [] });
    await flushWhenRequested(http, EPISODES_URL, { items: [] });

    store.resetZoom();

    expect(store.range()).toEqual({ kind: 'preset', preset: ChartRange.Hour });
    expect(store.zoomBase()).toBeNull();
    await flushWhenRequested(http, HISTORY_URL, { buckets: [] });
    await flushWhenRequested(http, EPISODES_URL, { items: [] });
  });

  it('signalsRoute_ChangeOfThePreset_ReloadsTheTimeInPhasesAndStatesOnceAfterThePause', async () => {
    const http = setup();
    const store = await openSignals(http);

    store.selectRange(ChartRange.Day);
    await TestBed.inject(ApplicationRef).whenStable();
    http.expectNone(PHASE_TIMES_URL);
    http.expectNone(STATE_TIMES_URL);

    await vi.waitFor(() => {
      const phases = http.expectOne((candidate) => candidate.url === PHASE_TIMES_URL);
      const span =
        Date.parse(phases.request.params.get('to') ?? '') - Date.parse(phases.request.params.get('from') ?? '');
      phases.flush(NO_PHASE_TIMES);
      expect(span).toBe(DAY_MS);
    });
    await vi.waitFor(() => {
      const states = http.expectOne((candidate) => candidate.url === STATE_TIMES_URL);
      const span =
        Date.parse(states.request.params.get('to') ?? '') - Date.parse(states.request.params.get('from') ?? '');
      states.flush(NO_STATE_TIMES);
      expect(span).toBe(DAY_MS);
    });
    await flushWhenRequested(http, EPISODES_URL, { items: [] });
  });

  it.each([
    ['AValidFromAndTo', '?from=2026-01-15T08:00:00Z&to=2026-01-15T09:00:00Z', true],
    ['AToBeforeTheFrom', '?from=2026-01-15T09:00:00Z&to=2026-01-15T08:00:00Z', false],
    ['TextThatIsNoInstant', '?from=abc&to=def', false],
    ['OnlyAFrom', '?from=2026-01-15T08:00:00Z', false],
  ])('signalsRoute_LinkWith%s_ShowsTheRangeOnlyWhenItIsValid', async (_name, query, isCustom) => {
    const http = setup();
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl(`/machines/m1/signals${query}`);
    await flushWhenRequested(http, MACHINE_URL, MACHINE);
    await navigation;
    const store = harness.routeDebugElement?.injector.get(MetricChartPageStore) as InstanceType<
      typeof MetricChartPageStore
    >;

    expect(store.range().kind === 'custom').toBe(isCustom);
  });

  it.each([
    ['AKnownMetricKey', '?metricKey=pressure', 'pressure'],
    ['AnUnknownMetricKey', '?metricKey=gone', 'temp'],
    ['NoMetricKey', '', 'temp'],
  ])('signalsRoute_LinkWith%s_SelectsThatMetricOrTheDefault', async (_name, query, expectedKey) => {
    const http = setup();
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl(`/machines/m1/signals${query}`);
    await flushWhenRequested(http, MACHINE_URL, MACHINE);
    await navigation;
    TestBed.tick();
    await flushWhenRequested(http, DEFINITIONS_URL, {
      revision: 1,
      definitions: [
        definition('flag', MetricValueKind.Boolean),
        definition('temp', MetricValueKind.Numeric),
        definition('pressure', MetricValueKind.Numeric),
      ],
    });
    const store = harness.routeDebugElement?.injector.get(MetricChartPageStore) as InstanceType<
      typeof MetricChartPageStore
    >;

    expect(store.selectedMetric()?.key).toBe(expectedKey);
  });

  it('signalsRoute_ZoomOfTheChart_LeavesTheTimeInPhasesAndStatesAlone', async () => {
    const http = setup();
    const store = await openSignals(http);

    store.zoomTo('2026-01-15T08:00:00.000Z', '2026-01-15T10:00:00.000Z');
    await flushWhenRequested(http, EPISODES_URL, { items: [] });

    http.expectNone(PHASE_TIMES_URL);
    http.expectNone(STATE_TIMES_URL);
    expect(store.statsRange()).toEqual({ kind: 'preset', preset: ChartRange.Hour });
  });

  it('signalsRoute_RefreshClickDuringTheCooldown_DoesNotReloadAgain', async () => {
    const http = setup();
    const store = await openSignals(http);
    store.setWidth(100);
    await flushWhenRequested(http, HISTORY_URL, { buckets: [] });

    store.refresh();
    await flushWhenRequested(http, HISTORY_URL, { buckets: [] });
    await flushWhenRequested(http, EPISODES_URL, { items: [] });
    await flushWhenRequested(http, PHASE_TIMES_URL, NO_PHASE_TIMES);
    await flushWhenRequested(http, STATE_TIMES_URL, NO_STATE_TIMES);
    store.refresh();
    await TestBed.inject(ApplicationRef).whenStable();

    http.expectNone(HISTORY_URL);
    expect(store.isRefreshCoolingDown()).toBe(true);
  });

  it('signalsRoute_MissingMachine_LeadsToTheNotFoundPage', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/machines/m1/signals');
    await vi.waitFor(() => {
      http.expectOne(MACHINE_URL).flush(null, { status: 404, statusText: 'Not Found' });
    });
    await navigation;

    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(NotFoundStub);
  });
});
