import { Component } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Location, PlatformNavigation } from '@angular/common';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';

import { APP_ACTION_HOSTS } from '@app/app-action-hosts';
import { APP_ROUTER_FEATURES } from '@app/app-router';
import { provideActionHosts } from '@shared/actions/action-registry';
import { ActionStore } from '@shared/actions/action.store';
import { HistoryStore } from '@shared/history/history.store';
import { reviewMetricDrafts } from '@machines/drafts/draft-review';
import { MachineActionType } from '@machines/machine-actions';
import { MACHINES_RETURN_TARGET } from '@machines/machines-return-target';
import type { FakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { provideFakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { CompanyStore } from '@shared/company/company.store';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { MachineState } from '@machines/machine';
import { MACHINES_ROUTES } from '@machines/machines.routes';
import { MetricsPage } from '@machines/metrics/metrics-page/metrics-page';
import { MetricsPageStore } from '@machines/metrics/metrics-page-store/metrics-page.store';
import { MetricValueKind } from '@machines/metrics/metric-definition';
import { StatusDefinitionsGridStore } from '@machines/statuses/list/status-definitions-grid.store';
import { StatusDefinitionsPage } from '@machines/statuses/list/status-definitions-page/status-definitions-page';
import { MachineFilesPage } from '@machines/files/machine-files-page/machine-files-page';
import { MachinesPage } from '@machines/list/machines-page/machines-page';

const MACHINE_URL = '/api/core/v1/machines/m1';
const MACHINES_SEARCH_URL = '/api/core/v1/machines/search';
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
      warningLow: null,
      warningHigh: null,
      alarmLow: null,
      alarmHigh: null,
      behavior: 'level',
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
        ...APP_ROUTER_FEATURES,
      ),
      provideFakePlatformNavigation(),
      provideActionHosts(APP_ACTION_HOSTS),
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

  it('machinesRoute_AfterLeavingAndReturning_SearchesWithoutTheEarlierFilter', async () => {
    vi.stubGlobal('ResizeObserver', ResizeObserverStub);
    const { http } = setup(true);
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/machines');
    TestBed.tick();
    await vi.waitFor(() => {
      expect(http.match(MACHINES_SEARCH_URL).length).toBeGreaterThan(0);
    });

    Reflect.get(harness.routeDebugElement?.componentInstance, 'store').setFilter({ name: 'lathe' });
    TestBed.tick();
    await vi.waitFor(() => {
      expect(http.match(MACHINES_SEARCH_URL).length).toBeGreaterThan(0);
    });

    await harness.navigateByUrl('/elsewhere');
    TestBed.tick();
    await harness.navigateByUrl('/machines');
    TestBed.tick();

    let returned: TestRequest[] = [];
    await vi.waitFor(() => {
      TestBed.tick();
      returned = http.match(MACHINES_SEARCH_URL);
      expect(returned.length).toBeGreaterThan(0);
    });
    expect(JSON.stringify(returned.at(-1)?.request.body)).not.toContain('lathe');
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
              warningLow: null,
              warningHigh: null,
              alarmLow: null,
              alarmHigh: null,
              behavior: null,
            },
          ],
          statuses: [],
          phases: [],
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

  async function openStatuses(
    http: HttpTestingController,
    url = '/machines/m1/statuses',
  ): Promise<{
    harness: RouterTestingHarness;
    store: InstanceType<typeof StatusDefinitionsGridStore>;
  }> {
    vi.stubGlobal('ResizeObserver', ResizeObserverStub);
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl(url);
    await flushWhenRequested(http, MACHINE_URL, MACHINE);
    await navigation;
    TestBed.tick();
    await vi.waitFor(() => {
      expect(http.match(STATUS_SEARCH_URL).length).toBeGreaterThan(0);
    });
    const store = harness.routeDebugElement?.injector.get(StatusDefinitionsGridStore);
    return { harness, store: store as InstanceType<typeof StatusDefinitionsGridStore> };
  }

  it('statusesRoute_OpenNewAndStored_PushCompose', async () => {
    const { http } = setup(true);
    const { store } = await openStatuses(http);
    const router = TestBed.inject(Router);
    const navigation = TestBed.inject(PlatformNavigation) as FakePlatformNavigation;
    const entryCount = navigation.entries().length;

    store.requestCreate();
    await vi.waitFor(() => expect(router.url).toBe('/machines/m1/statuses?compose=status'));
    const createdEditor = store.editor();
    store.closeEditor();
    await vi.waitFor(() => expect(router.url).toBe('/machines/m1/statuses'));
    store.requestUpdate('s1');
    await vi.waitFor(() => expect(router.url).toBe('/machines/m1/statuses?compose=status:s1'));

    expect(createdEditor).toEqual({ statusDefinitionId: null });
    expect(store.editor()).toEqual({ statusDefinitionId: 's1' });
    expect(navigation.entries().length).toBe(entryCount + 1);
  });

  it('statusesRoute_ComposeOnLoad_OpensTheDialog', async () => {
    const { http } = setup(true);

    const { store } = await openStatuses(http, '/machines/m1/statuses?compose=status:s1');
    await new Promise((resolve) => setTimeout(resolve));

    expect(store.editor()).toEqual({ statusDefinitionId: 's1' });
    expect(TestBed.inject(Router).url).toBe('/machines/m1/statuses?compose=status:s1');
  });

  it('statusesRoute_ComposeChange_RunsNoResolver', async () => {
    const { http } = setup(true);
    const { store } = await openStatuses(http);
    http.match(STATUS_SEARCH_URL);

    store.requestCreate();
    await vi.waitFor(() => expect(store.editor()).not.toBeNull());
    TestBed.tick();

    http.expectNone(MACHINE_URL);
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

  it('metricsRoute_BackWithStagedChangesAndCancel_KeepsPageAndEntries', async () => {
    const { http, confirm } = setup(false);
    await TestBed.inject(Router).navigateByUrl('/not-found');
    const harness = await openMetrics(http);
    const navigation = TestBed.inject(PlatformNavigation) as FakePlatformNavigation;
    const store = harness.routeDebugElement?.injector.get(MetricsPageStore);
    store?.remove('a');
    const urlsBefore = navigation.entries().map((entry) => entry.url);
    const indexBefore = navigation.currentEntry.index;

    store?.back();
    await vi.waitFor(() => {
      expect(confirm).toHaveBeenCalledTimes(1);
    });

    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(MetricsPage);
    expect(store?.hasChanges()).toBe(true);
    expect(navigation.entries().map((entry) => entry.url)).toEqual(urlsBefore);
    expect(navigation.currentEntry.index).toBe(indexBefore);
  });

  it('metricsRoute_ApplyChangesWithPreviousEntry_GoesBackWithoutTheLeaveQuestion', async () => {
    const { http, confirm } = setup(true);
    await TestBed.inject(Router).navigateByUrl('/not-found');
    const harness = await openMetrics(http);
    const store = harness.routeDebugElement?.injector.get(MetricsPageStore);
    store?.remove('a');

    store?.apply();
    await flushWhenRequested(http, DEFINITIONS_URL, { revision: 3 });
    await vi.waitFor(() => {
      expect(TestBed.inject(Router).url).toBe('/not-found');
    });

    expect(confirm).not.toHaveBeenCalled();
  });

  it('metricsRoute_DiscardWithPreviousEntry_AsksOnceAndGoesBack', async () => {
    const { http, confirm } = setup(true);
    await TestBed.inject(Router).navigateByUrl('/not-found');
    const harness = await openMetrics(http);
    const store = harness.routeDebugElement?.injector.get(MetricsPageStore);
    store?.remove('a');

    store?.requestDiscard();
    await vi.waitFor(() => {
      expect(TestBed.inject(Router).url).toBe('/not-found');
    });

    expect(confirm).toHaveBeenCalledTimes(1);
  });

  it('metricsRoute_DiscardWithPendingDraftsAndPreviousEntry_AsksOnceAndGoesBack', async () => {
    const { http, confirm } = setup(true);
    await TestBed.inject(Router).navigateByUrl('/not-found');
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
              warningLow: null,
              warningHigh: null,
              alarmLow: null,
              alarmHigh: null,
              behavior: null,
            },
          ],
          statuses: [],
          phases: [],
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
  });

  describe('action drawers', () => {
    const DRAWER = { type: MachineActionType.Update, payload: { machineId: 'm1' } };
    const DRAFTS = { type: MachineActionType.Drafts, payload: { machineId: 'm1' } };

    async function openList(http: HttpTestingController, url = '/machines'): Promise<RouterTestingHarness> {
      vi.stubGlobal('ResizeObserver', ResizeObserverStub);
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl(url);
      TestBed.tick();
      await vi.waitFor(() => {
        expect(http.match(MACHINES_SEARCH_URL).length).toBeGreaterThan(0);
      });
      return harness;
    }

    function navigationOf(): FakePlatformNavigation {
      return TestBed.inject(PlatformNavigation) as FakePlatformNavigation;
    }

    it('machinesRoute_OpenDrawer_PushesComposeAndKeepsGridState', async () => {
      const { http } = setup(true);
      const harness = await openList(http);
      const store = Reflect.get(harness.routeDebugElement?.componentInstance, 'store');
      store.setFilter({ name: 'lathe' });
      TestBed.tick();
      await vi.waitFor(() => {
        expect(http.match(MACHINES_SEARCH_URL).length).toBeGreaterThan(0);
      });
      const entryCount = navigationOf().entries().length;
      const actions = TestBed.inject(ActionStore);

      await actions.dispatch(DRAWER);
      await vi.waitFor(() => expect(actions.active()).toEqual(DRAWER));

      expect(TestBed.inject(Router).url).toBe('/machines?compose=machine:m1');
      expect(navigationOf().entries().length).toBe(entryCount + 1);
      expect(JSON.stringify(store.filter())).toContain('lathe');
      expect(http.match(MACHINES_SEARCH_URL).length).toBe(0);
    });

    it('machinesRoute_CloseDrawer_GoesBackOneEntry', async () => {
      const { http } = setup(true);
      await openList(http);
      const actions = TestBed.inject(ActionStore);
      await actions.dispatch(DRAWER);
      await vi.waitFor(() => expect(actions.active()).toEqual(DRAWER));
      const entryCount = navigationOf().entries().length;

      actions.cancel();
      await vi.waitFor(() => expect(TestBed.inject(Router).url).toBe('/machines'));

      expect(navigationOf().entries().length).toBe(entryCount);
      expect(navigationOf().currentEntry.index).toBe(entryCount - 2);
      expect(http.match(MACHINES_SEARCH_URL).length).toBe(0);
    });

    it('machinesRoute_BrowserBackOnDrawer_ClosesItWithoutResolver', async () => {
      const { http } = setup(true);
      await openList(http);
      const actions = TestBed.inject(ActionStore);
      await actions.dispatch(DRAWER);
      await vi.waitFor(() => expect(actions.active()).toEqual(DRAWER));

      TestBed.inject(Location).back();
      await vi.waitFor(() => expect(actions.active()).toBeNull());

      expect(TestBed.inject(Router).url).toBe('/machines');
      http.expectNone(MACHINE_URL);
      expect(http.match(MACHINES_SEARCH_URL).length).toBe(0);
    });

    it('machinesRoute_DeepLinkCompose_OpensAndClosesWithReplace', async () => {
      const { http } = setup(true);
      const actions = TestBed.inject(ActionStore);
      await TestBed.inject(Router).navigateByUrl('/elsewhere');
      await openList(http, '/machines?compose=machine:m1');
      await vi.waitFor(() => expect(actions.active()).toEqual(DRAWER));
      const entryCount = navigationOf().entries().length;

      actions.cancel();
      await vi.waitFor(() => expect(TestBed.inject(Router).url).toBe('/machines'));

      expect(navigationOf().entries().length).toBe(entryCount);
    });

    it('machinesRoute_DraftsListChild_OpensInMemoryAndReturns', async () => {
      const { http } = setup(true);
      await openList(http);
      const actions = TestBed.inject(ActionStore);
      await actions.dispatch(DRAFTS);
      await vi.waitFor(() => expect(actions.active()).toEqual(DRAFTS));
      const entryCount = navigationOf().entries().length;

      await actions.dispatchChild({ type: MachineActionType.Create, payload: { draftId: 'd1' } });
      const childType = actions.active()?.type;
      actions.complete(MachineActionType.Saved);

      expect(childType).toBe(MachineActionType.Create);
      expect(actions.active()).toEqual(DRAFTS);
      expect(actions.lastResult()?.type).toBe(MachineActionType.Saved);
      expect(navigationOf().entries().length).toBe(entryCount);
      expect(TestBed.inject(Router).url).toBe('/machines?compose=machine-drafts:m1');
    });

    it('machinesRoute_DraftsListToMetricsReview_BackReopensTheList', async () => {
      const { http } = setup(true);
      await openList(http);
      const actions = TestBed.inject(ActionStore);
      await actions.dispatch(DRAFTS);
      await vi.waitFor(() => expect(actions.active()).toEqual(DRAFTS));

      const navigation = reviewMetricDrafts(TestBed.inject(Router), 'm1', 'a1');
      await flushWhenRequested(http, MACHINE_URL, MACHINE);
      await navigation;
      TestBed.tick();
      await flushWhenRequested(http, DEFINITIONS_URL, DEFINITIONS);
      await flushWhenRequested(http, UNITS_URL, { kinds: [] });
      await flushWhenRequested(http, ATTEMPT_DRAFTS_URL, { groups: [] });
      const activeOnMetrics = actions.active();
      const entryCount = navigationOf().entries().length;

      await TestBed.inject(HistoryStore).back(MACHINES_RETURN_TARGET);
      await vi.waitFor(() => expect(actions.active()).toEqual(DRAFTS));

      expect(activeOnMetrics).toBeNull();
      expect(TestBed.inject(Router).url).toBe('/machines?compose=machine-drafts:m1');
      expect(navigationOf().entries().length).toBe(entryCount);
    });
  });
});
