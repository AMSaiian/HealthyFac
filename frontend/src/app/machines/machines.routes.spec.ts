import { Component } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';

import { AuthStore } from '@shared/security/auth.store';
import { MachineState } from '@machines/machine';
import { MACHINES_ROUTES } from '@machines/machines.routes';
import { MetricsPage } from '@machines/metrics/metrics-page/metrics-page';
import { MetricsPageStore } from '@machines/metrics/metrics-page.store';
import { MetricValueKind } from '@machines/metrics/metric-definition';
import { StatusDefinitionsPage } from '@machines/statuses/list/status-definitions-page/status-definitions-page';

const MACHINE_URL = '/api/core/v1/machines/m1';
const DEFINITIONS_URL = '/api/telemetry/v1/machines/m1/metric-definitions';
const UNITS_URL = '/api/telemetry/v1/metric-units';
const STATUS_SEARCH_URL = '/api/telemetry/v1/machines/m1/status-definitions/search';

const MACHINE = {
  id: 'm1',
  name: 'Lathe',
  serialNumber: 'S1',
  gatewayId: 'abcdefghijklmnop',
  departmentId: 'd1',
  state: MachineState.Operational,
  description: null,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: null,
};

const DEFINITIONS = {
  revision: 2,
  definitions: [
    {
      id: 'a',
      key: 'spindle.temperature',
      name: 'Spindle temperature',
      description: null,
      valueKind: MetricValueKind.Boolean,
      unit: { quantityKind: 'Discrete', name: 'Boolean', abbreviation: '' },
      min: null,
      max: null,
      states: null,
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: null,
    },
  ],
};

@Component({ template: '', selector: 'hf-elsewhere', host: { class: 'contents' } })
class Elsewhere {}

function setup(isConfirmed: boolean): { http: HttpTestingController; confirm: ReturnType<typeof vi.fn> } {
  const confirm = vi.fn((options: { accept: () => void; reject?: () => void }) =>
    isConfirmed ? options.accept() : options.reject?.(),
  );
  TestBed.configureTestingModule({
    providers: [
      provideRouter([
        { path: 'machines', children: MACHINES_ROUTES },
        { path: 'elsewhere', component: Elsewhere },
      ]),
      provideHttpClient(),
      provideHttpClientTesting(),
      provideTranslateService(),
      { provide: AuthStore, useValue: { hasPermission: () => true } },
      { provide: MessageService, useValue: { add: vi.fn() } },
      { provide: ConfirmationService, useValue: { confirm } },
    ],
  });
  return { http: TestBed.inject(HttpTestingController), confirm };
}

async function flushWhenRequested(http: HttpTestingController, url: string, body: object): Promise<void> {
  await vi.waitFor(() => {
    http.expectOne(url).flush(body);
  });
}

async function openMetrics(http: HttpTestingController): Promise<RouterTestingHarness> {
  const harness = await RouterTestingHarness.create();
  const navigation = harness.navigateByUrl('/machines/m1/metrics');
  await flushWhenRequested(http, MACHINE_URL, MACHINE);
  await navigation;
  TestBed.tick();
  await flushWhenRequested(http, DEFINITIONS_URL, DEFINITIONS);
  await flushWhenRequested(http, UNITS_URL, { kinds: [] });
  await TestBed.inject(ApplicationRef).whenStable();
  return harness;
}

class ResizeObserverStub {
  observe = vi.fn();
  unobserve = vi.fn();
  disconnect = vi.fn();
}

describe('machines routes', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('metricsRoute_OnOpen_RequestsDefinitionsAndUnitsAndActivatesThePage', async () => {
    const { http } = setup(true);

    const harness = await openMetrics(http);

    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(MetricsPage);
    const store = harness.routeDebugElement?.injector.get(MetricsPageStore);
    expect(store?.revision()).toBe(2);
    expect(store?.selectedId()).toBe('a');
    http.verify();
  });

  it('metricsRoute_LeavingWithStagedChangesAndReject_KeepsThePage', async () => {
    const { http, confirm } = setup(false);
    const harness = await openMetrics(http);
    harness.routeDebugElement?.injector.get(MetricsPageStore).remove('a');

    await TestBed.inject(Router).navigateByUrl('/elsewhere');

    expect(confirm).toHaveBeenCalledTimes(1);
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(MetricsPage);
  });

  it('metricsRoute_LeavingWithStagedChangesAndAccept_LeavesThePage', async () => {
    const { http, confirm } = setup(true);
    const harness = await openMetrics(http);
    harness.routeDebugElement?.injector.get(MetricsPageStore).remove('a');

    await TestBed.inject(Router).navigateByUrl('/elsewhere');

    expect(confirm).toHaveBeenCalledTimes(1);
    expect(TestBed.inject(Router).url).toBe('/elsewhere');
  });

  it('metricsRoute_LeavingWithoutChanges_DoesNotAsk', async () => {
    const { http, confirm } = setup(false);
    await openMetrics(http);

    await TestBed.inject(Router).navigateByUrl('/elsewhere');

    expect(confirm).not.toHaveBeenCalled();
    expect(TestBed.inject(Router).url).toBe('/elsewhere');
  });

  it('statusesRoute_OnOpen_SearchesOnTheTelemetryPathAndActivatesThePage', async () => {
    vi.stubGlobal('ResizeObserver', ResizeObserverStub);
    const { http } = setup(true);
    const harness = await RouterTestingHarness.create();

    const navigation = harness.navigateByUrl('/machines/m1/statuses');
    await flushWhenRequested(http, MACHINE_URL, MACHINE);
    await navigation;
    TestBed.tick();
    await vi.waitFor(() => {
      expect(http.match(STATUS_SEARCH_URL).length).toBeGreaterThan(0);
    });

    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(StatusDefinitionsPage);
  });
});
