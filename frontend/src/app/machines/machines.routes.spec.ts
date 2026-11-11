import { Component } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';

import { ActionStore } from '@shared/actions/action.store';
import { CompanyStore } from '@shared/company/company.store';
import { AuthStore } from '@shared/security/auth.store';
import { MachineState } from '@machines/machine';
import { MACHINES_ROUTES } from '@machines/machines.routes';
import { MetricsPage } from '@machines/metrics/metrics-page/metrics-page';
import { MetricsPageStore } from '@machines/metrics/metrics-page.store';
import { MetricValueKind } from '@machines/metrics/metric-definition';
import { StatusDefinitionsPage } from '@machines/statuses/list/status-definitions-page/status-definitions-page';
import { MachineFilesPage } from '@machines/files/machine-files-page/machine-files-page';
import { MachinesPage } from '@machines/list/machines-page/machines-page';

const MACHINE_URL = '/api/core/v1/machines/m1';
const DEFINITIONS_URL = '/api/telemetry/v1/machines/m1/metric-definitions';
const UNITS_URL = '/api/telemetry/v1/metric-units';
const FILES_SEARCH_URL = '/api/document/v1/machines/m1/files/search';
const COMPANY_FILES_SEARCH_URL = '/api/document/v1/files/search';
const ATTEMPT_DRAFTS_URL = '/api/core/v1/ingest-attempts/a1/drafts';
const STATUS_SEARCH_URL = '/api/telemetry/v1/machines/m1/status-definitions/search';

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

@Component({ template: '', selector: 'hf-not-found-stub', host: { class: 'contents' } })
class NotFoundStub {}

function setup(isConfirmed: boolean): { http: HttpTestingController; confirm: ReturnType<typeof vi.fn> } {
  const confirm = vi.fn((options: { accept: () => void; reject?: () => void }) =>
    isConfirmed ? options.accept() : options.reject?.(),
  );
  TestBed.configureTestingModule({
    providers: [
      provideRouter(
        [
          { path: 'machines', children: MACHINES_ROUTES },
          { path: 'elsewhere', component: Elsewhere },
          { path: 'not-found', component: NotFoundStub },
        ],
        withComponentInputBinding(),
      ),
      provideHttpClient(),
      provideHttpClientTesting(),
      provideTranslateService(),
      { provide: AuthStore, useValue: { hasPermission: () => true } },
      { provide: CompanyStore, useValue: { timeZoneId: () => 'Europe/Kyiv' } },
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

  it('metricsRoute_WithDraftAttemptQuery_RequestsTheDraftsAndStagesThemOnThePage', async () => {
    const { http } = setup(true);
    const harness = await RouterTestingHarness.create();

    const navigation = harness.navigateByUrl('/machines/m1/metrics?draftAttemptId=a1');
    await flushWhenRequested(http, MACHINE_URL, MACHINE);
    await navigation;
    TestBed.tick();
    await flushWhenRequested(http, DEFINITIONS_URL, DEFINITIONS);
    await flushWhenRequested(http, UNITS_URL, { kinds: [] });
    await flushWhenRequested(http, ATTEMPT_DRAFTS_URL, {
      groups: [
        {
          machineId: 'm1',
          machineName: 'Lathe',
          machineDraft: null,
          metrics: [
            {
              id: 'draft-1',
              state: 'pending',
              sourceLocation: 'page 3',
              key: 'spindle.temperature',
              name: 'Spindle temperature (proposed)',
              description: null,
              valueKind: 'boolean',
              quantityKindName: null,
              unitName: null,
              min: null,
              max: null,
              states: null,
            },
          ],
          statuses: [],
        },
      ],
    });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    TestBed.tick();

    const store = harness.routeDebugElement?.injector.get(MetricsPageStore);
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(MetricsPage);
    expect(store?.items()[0]).toMatchObject({ draftId: 'draft-1', sourceLocation: 'page 3' });
    expect(store?.items()[0].value.name).toBe('Spindle temperature (proposed)');
    expect(store?.hasChanges()).toBe(true);
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

  it('filesRoute_OnOpen_LoadsTheMachineSearchesItsFilesAndActivatesThePage', async () => {
    vi.stubGlobal('ResizeObserver', ResizeObserverStub);
    const { http } = setup(true);
    const harness = await RouterTestingHarness.create();

    const navigation = harness.navigateByUrl('/machines/m1/files');
    await flushWhenRequested(http, MACHINE_URL, MACHINE);
    await navigation;
    TestBed.tick();
    await vi.waitFor(() => {
      expect(http.match(FILES_SEARCH_URL).length).toBe(1);
    });

    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(MachineFilesPage);
    http.expectNone(COMPANY_FILES_SEARCH_URL);
  });

  it('filesRoute_WithUnknownMachine_ActivatesTheNotFoundPage', async () => {
    const { http } = setup(true);
    const harness = await RouterTestingHarness.create();

    const navigation = harness.navigateByUrl('/machines/m1/files');
    await vi.waitFor(() => {
      http.expectOne(MACHINE_URL).flush(null, { status: 404, statusText: 'Not Found' });
    });
    await navigation;

    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(NotFoundStub);
    http.expectNone(FILES_SEARCH_URL);
  });

  it('filesRoute_NavigatingBackToMachines_ActivatesTheMachinesPage', async () => {
    vi.stubGlobal('ResizeObserver', ResizeObserverStub);
    const { http } = setup(true);
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/machines/m1/files');
    await flushWhenRequested(http, MACHINE_URL, MACHINE);
    await navigation;
    TestBed.tick();

    const back = harness.navigateByUrl('/machines');
    TestBed.tick();
    await back;

    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(MachinesPage);
  });

  it('metricsRoute_BackWithReturnPath_OpensTheStoredDialogOnThePageThatOpenedTheReview', async () => {
    const { http } = setup(true);
    const actions = TestBed.inject(ActionStore);
    const dialog = { type: 'files.ingestSummary', payload: { fileId: 'f1' } };
    await TestBed.inject(Router).navigateByUrl('/not-found');
    actions.rememberReturn('/not-found', dialog, '/machines/m1/metrics');
    const harness = await openMetrics(http);

    harness.routeDebugElement?.injector.get(MetricsPageStore).back();
    await vi.waitFor(() => {
      expect(TestBed.inject(Router).url).toBe('/not-found');
    });

    expect(actions.active()).toEqual(dialog);
    expect(actions.returnPath()).toBeNull();
  });

  it('metricsRoute_LeavingToAnotherPage_DropsTheReturnPath', async () => {
    const { http } = setup(true);
    const actions = TestBed.inject(ActionStore);
    const dialog = { type: 'files.ingestSummary', payload: { fileId: 'f1' } };
    await TestBed.inject(Router).navigateByUrl('/not-found');
    actions.rememberReturn('/not-found', dialog, '/machines/m1/metrics');
    await openMetrics(http);

    await TestBed.inject(Router).navigateByUrl('/elsewhere');
    await TestBed.inject(Router).navigateByUrl('/not-found');

    expect(actions.returnPath()).toBeNull();
    expect(actions.active()).toBeNull();
  });

  it('metricsRoute_ApplyChangesWithReturnPath_LeavesWithoutAskingAndOpensTheStoredDialog', async () => {
    const { http, confirm } = setup(true);
    const actions = TestBed.inject(ActionStore);
    const dialog = { type: 'files.ingestSummary', payload: { fileId: 'f1' } };
    await TestBed.inject(Router).navigateByUrl('/not-found');
    actions.rememberReturn('/not-found', dialog, '/machines/m1/metrics');
    const harness = await openMetrics(http);
    const store = harness.routeDebugElement?.injector.get(MetricsPageStore);
    store?.remove('a');

    store?.apply();
    await flushWhenRequested(http, DEFINITIONS_URL, { revision: 3 });
    await flushWhenRequested(http, DEFINITIONS_URL, { revision: 3, definitions: [] });
    await vi.waitFor(() => {
      expect(TestBed.inject(Router).url).toBe('/not-found');
    });

    expect(confirm).not.toHaveBeenCalled();
    expect(actions.active()).toEqual(dialog);
    expect(actions.returnPath()).toBeNull();
  });

  it('metricsRoute_DiscardWithReturnPath_AsksOnceAndOpensTheStoredDialog', async () => {
    const { http, confirm } = setup(true);
    const actions = TestBed.inject(ActionStore);
    const dialog = { type: 'files.ingestSummary', payload: { fileId: 'f1' } };
    await TestBed.inject(Router).navigateByUrl('/not-found');
    actions.rememberReturn('/not-found', dialog, '/machines/m1/metrics');
    const harness = await openMetrics(http);
    const store = harness.routeDebugElement?.injector.get(MetricsPageStore);
    store?.remove('a');

    store?.requestDiscard();
    await flushWhenRequested(http, DEFINITIONS_URL, { ...DEFINITIONS });
    await vi.waitFor(() => {
      expect(TestBed.inject(Router).url).toBe('/not-found');
    });

    expect(confirm).toHaveBeenCalledTimes(1);
    expect(actions.active()).toEqual(dialog);
  });

  it('metricsRoute_DiscardWithReturnPathAndPendingDrafts_AsksOnceAndOpensTheStoredDialog', async () => {
    const { http, confirm } = setup(true);
    const actions = TestBed.inject(ActionStore);
    const dialog = { type: 'files.ingestSummary', payload: { fileId: 'f1' } };
    await TestBed.inject(Router).navigateByUrl('/not-found');
    actions.rememberReturn('/not-found', dialog, '/machines/m1/metrics');
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/machines/m1/metrics?draftAttemptId=a1');
    await flushWhenRequested(http, MACHINE_URL, MACHINE);
    await navigation;
    TestBed.tick();
    await flushWhenRequested(http, DEFINITIONS_URL, DEFINITIONS);
    await flushWhenRequested(http, UNITS_URL, { kinds: [] });
    await flushWhenRequested(http, ATTEMPT_DRAFTS_URL, {
      groups: [
        {
          machineId: 'm1',
          machineName: 'Lathe',
          machineDraft: null,
          metrics: [
            {
              id: 'draft-1',
              state: 'pending',
              sourceLocation: 'page 3',
              key: 'new.key',
              name: 'New metric',
              description: null,
              valueKind: 'boolean',
              quantityKindName: null,
              unitName: null,
              min: null,
              max: null,
              states: null,
            },
          ],
          statuses: [],
        },
      ],
    });
    await TestBed.inject(ApplicationRef).whenStable();
    const store = harness.routeDebugElement?.injector.get(MetricsPageStore);
    expect(store?.hasPendingWork()).toBe(true);

    store?.requestDiscard();
    await vi.waitFor(() => {
      expect(TestBed.inject(Router).url).toBe('/not-found');
    });

    expect(confirm).toHaveBeenCalledTimes(1);
    expect(actions.active()).toEqual(dialog);
  });
});
