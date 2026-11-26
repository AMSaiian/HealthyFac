import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PlatformNavigation } from '@angular/common';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';

import { APP_ROUTER_FEATURES } from '@app/app-router';
import { CompanyStore } from '@shared/company/company.store';
import type { FakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { provideFakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { DetailsMode } from '@shared/forms/with-details-mode';
import { provideFakeAuthStore } from '@shared/security/testing/fake-auth-store';
import { Permission } from '@shared/security/permissions';

import { MachineState } from '@machines/machine';
import { MACHINES_ROUTES } from '@machines/machines.routes';
import { PhaseConditionOperator } from '@machines/phases/operating-phase';
import { OperatingPhasesPage } from '@machines/phases/operating-phases-page/operating-phases-page';
import { OperatingPhasesPageStore } from '@machines/phases/operating-phases-page-store/operating-phases-page.store';
import { EMPTY_FORM_VALUE, toFormValue } from '@machines/phases/phase-form/phase-form.utils';
import type { OperatingPhase } from '@machines/phases/operating-phase';

const MACHINE_URL = '/api/core/v1/machines/m1';
const PHASES_URL = '/api/telemetry/v1/machines/m1/operating-phases';
const ORDER_URL = `${PHASES_URL}/order`;
const DEFINITIONS_URL = '/api/telemetry/v1/machines/m1/metric-definitions';
const STATUS_SEARCH_URL = '/api/telemetry/v1/machines/m1/status-definitions/search';
const LARGEST_API_PAGE = 100;
let statusSearchLimit = 0;

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

function phase(id: string, priority: number): object {
  return {
    id,
    code: `code.${id}`,
    name: `Phase ${id}`,
    description: null,
    priority,
    isTracked: true,
    conditions: [],
    limits: [],
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: null,
  };
}

@Component({ template: '', selector: 'hf-not-found-stub', host: { class: 'contents' } })
class NotFoundStub {}

function setup(permissions: readonly Permission[] | null = null): HttpTestingController {
  TestBed.configureTestingModule({
    providers: [
      provideRouter(
        [
          { path: 'machines', children: MACHINES_ROUTES },
          { path: 'not-found', component: NotFoundStub },
        ],
        ...APP_ROUTER_FEATURES,
      ),
      provideFakePlatformNavigation(),
      provideHttpClient(),
      provideHttpClientTesting(),
      provideTranslateService(),
      provideFakeAuthStore({
        hasPermission: (permission: Permission) => permissions === null || permissions.includes(permission),
      }),
      { provide: CompanyStore, useValue: { timeZoneId: () => 'Europe/Kyiv' } },
      { provide: MessageService, useValue: { add: vi.fn() } },
      { provide: ConfirmationService, useValue: { confirm: vi.fn() } },
    ],
  });
  return TestBed.inject(HttpTestingController);
}

async function flushWhenRequested(http: HttpTestingController, url: string, body: object): Promise<void> {
  await vi.waitFor(() => {
    http.expectOne(url).flush(body);
  });
}

async function openPhases(http: HttpTestingController, url = '/machines/m1/phases'): Promise<RouterTestingHarness> {
  const harness = await RouterTestingHarness.create();
  const navigation = harness.navigateByUrl(url);
  await flushWhenRequested(http, MACHINE_URL, MACHINE);
  await navigation;
  TestBed.tick();
  await flushWhenRequested(http, PHASES_URL, { items: [phase('a', 1), phase('b', 3), phase('c', 2)] });
  await flushWhenRequested(http, DEFINITIONS_URL, { revision: 1, definitions: [] });
  await vi.waitFor(() => {
    const search = http.expectOne(STATUS_SEARCH_URL);
    statusSearchLimit = search.request.body.limit;
    search.flush({ items: [], totalCount: 0 });
  });
  await TestBed.inject(ApplicationRef).whenStable();
  return harness;
}

describe('phases route', { tags: ['SignalModel'] }, () => {
  it('phasesRoute_AfterOpening_AsksForTheStatusesWithAPageThatTheApiAccepts', async () => {
    const http = setup();

    await openPhases(http);

    expect(statusSearchLimit).toBeGreaterThan(0);
    expect(statusSearchLimit).toBeLessThanOrEqual(LARGEST_API_PAGE);
  });

  it('phasesRoute_AfterOpening_ListsThePhasesWithTheLowestPriorityFirst', async () => {
    const http = setup();
    const harness = await openPhases(http);

    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(OperatingPhasesPage);
    const store = TestBed.runInInjectionContext(() =>
      harness.routeDebugElement?.injector.get(OperatingPhasesPageStore),
    );
    expect(store?.items().map((item: { id: string }) => item.id)).toEqual(['a', 'c', 'b']);
  });

  it('phasesRoute_ReorderOfTheFirstRowToTheLast_SendsTheIdsInTheNewOrderAndShowsThatOrderAfterTheReload', async () => {
    const http = setup();
    const harness = await openPhases(http);
    const store = harness.routeDebugElement?.injector.get(OperatingPhasesPageStore);

    store?.reorder(0, 2);

    await vi.waitFor(() => {
      const request = http.expectOne(ORDER_URL);
      expect(request.request.method).toBe('PUT');
      expect(request.request.body).toEqual({ phaseIds: ['c', 'b', 'a'] });
      request.flush(null);
    });
    await flushWhenRequested(http, PHASES_URL, { items: [phase('c', 0), phase('b', 1), phase('a', 2)] });
    await TestBed.inject(ApplicationRef).whenStable();

    expect(store?.items().map((item: { id: string }) => item.id)).toEqual(['c', 'b', 'a']);
  });

  it('phasesRoute_SaveOfANewPhase_SendsThePriorityAboveTheHighestStoredOne', async () => {
    const http = setup();
    const harness = await openPhases(http);
    const store = harness.routeDebugElement?.injector.get(OperatingPhasesPageStore);

    store?.requestCreate();
    await vi.waitFor(() => expect(store?.editor()).not.toBeNull());
    TestBed.tick();
    store?.save({
      value: { ...EMPTY_FORM_VALUE, code: 'idle', name: 'Idle', conditions: [] },
      onSaved: vi.fn(),
    });

    await vi.waitFor(() => {
      const request = http.expectOne(PHASES_URL);
      expect(request.request.method).toBe('POST');
      expect(request.request.body).toMatchObject({ code: 'idle', priority: 4, description: null, conditions: [] });
      request.flush({ id: 'new' });
    });
  });

  it('phasesRoute_ConditionOfAStatus_SendsTheStatusIdAndNoMetricId', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/machines/m1/phases');
    await flushWhenRequested(http, MACHINE_URL, MACHINE);
    await navigation;
    TestBed.tick();
    await flushWhenRequested(http, PHASES_URL, { items: [] });
    await flushWhenRequested(http, DEFINITIONS_URL, { revision: 1, definitions: [] });
    await vi.waitFor(() => {
      http.expectOne(STATUS_SEARCH_URL).flush({ items: [{ id: 's1', code: 'RUN' }], totalCount: 1 });
    });
    const store = harness.routeDebugElement?.injector.get(OperatingPhasesPageStore);

    store?.requestCreate();
    await vi.waitFor(() => expect(store?.editor()).not.toBeNull());
    TestBed.tick();
    store?.save({
      value: {
        ...EMPTY_FORM_VALUE,
        code: 'run',
        name: 'Run',
        conditions: [
          { sourceId: 's1', operator: PhaseConditionOperator.Active, value: null, upperValue: null, states: [] },
        ],
      },
      onSaved: vi.fn(),
    });

    await vi.waitFor(() => {
      const request = http.expectOne(PHASES_URL);
      expect(request.request.body.conditions).toEqual([
        {
          metricDefinitionId: null,
          statusDefinitionId: 's1',
          operator: 'active',
          value: null,
          upperValue: null,
          states: null,
        },
      ]);
      request.flush({ id: 'new' });
    });
  });

  it('phasesRoute_ShowDetailsOfAPhase_OpensTheDialogInViewMode', async () => {
    const http = setup();
    const harness = await openPhases(http);
    const store = harness.routeDebugElement?.injector.get(OperatingPhasesPageStore);

    store?.requestUpdate('a');
    await vi.waitFor(() => expect(store?.editor()).not.toBeNull());
    TestBed.tick();

    expect(store?.editor()).toEqual({ phaseId: 'a' });
    expect(store?.mode()).toBe(DetailsMode.View);
    expect(store?.isEditShown()).toBe(true);
  });

  it('phasesRoute_AddNew_OpensTheDialogInEditMode', async () => {
    const http = setup();
    const harness = await openPhases(http);
    const store = harness.routeDebugElement?.injector.get(OperatingPhasesPageStore);

    store?.requestCreate();
    await vi.waitFor(() => expect(store?.editor()).not.toBeNull());
    TestBed.tick();

    expect(store?.editor()).toEqual({ phaseId: null });
    expect(store?.mode()).toBe(DetailsMode.Edit);
    expect(store?.isEditShown()).toBe(false);
  });

  it('phasesRoute_PencilThenSave_SendsThePutReloadsTheListAndStaysOpenInViewMode', async () => {
    const http = setup();
    const harness = await openPhases(http);
    const store = harness.routeDebugElement?.injector.get(OperatingPhasesPageStore);
    store?.requestUpdate('a');
    await vi.waitFor(() => expect(store?.editor()).not.toBeNull());
    TestBed.tick();
    store?.edit();
    const edited = store?.editedPhase() as OperatingPhase;

    store?.save({ value: { ...toFormValue(edited), name: 'Renamed' }, onSaved: () => store.onSaved() });

    await vi.waitFor(() => {
      const request = http.expectOne(`${PHASES_URL}/a`);
      expect(request.request.method).toBe('PUT');
      expect(request.request.body).toMatchObject({ name: 'Renamed' });
      request.flush(null);
    });
    await flushWhenRequested(http, PHASES_URL, {
      items: [{ ...phase('a', 1), name: 'Renamed' }, phase('b', 3), phase('c', 2)],
    });
    await TestBed.inject(ApplicationRef).whenStable();

    expect(store?.mode()).toBe(DetailsMode.View);
    expect(store?.editor()).toEqual({ phaseId: 'a' });
    expect(store?.editedPhase()?.name).toBe('Renamed');
  });

  it('phasesRoute_SaveWith409_StaysInEditMode', async () => {
    const http = setup();
    const harness = await openPhases(http);
    const store = harness.routeDebugElement?.injector.get(OperatingPhasesPageStore);
    store?.requestUpdate('a');
    await vi.waitFor(() => expect(store?.editor()).not.toBeNull());
    TestBed.tick();
    store?.edit();
    const edited = store?.editedPhase() as OperatingPhase;

    store?.save({ value: { ...toFormValue(edited), code: 'taken' }, onSaved: () => store.onSaved() });

    await vi.waitFor(() => {
      http.expectOne(`${PHASES_URL}/a`).flush(null, { status: 409, statusText: 'Conflict' });
    });
    await TestBed.inject(ApplicationRef).whenStable();

    expect(store?.mode()).toBe(DetailsMode.Edit);
    expect(store?.editor()).toEqual({ phaseId: 'a' });
  });

  it('phasesRoute_PencilThenCancel_ReturnsToViewMode', async () => {
    const http = setup();
    const harness = await openPhases(http);
    const store = harness.routeDebugElement?.injector.get(OperatingPhasesPageStore);
    store?.requestUpdate('a');
    await vi.waitFor(() => expect(store?.editor()).not.toBeNull());
    TestBed.tick();
    store?.edit();

    store?.cancelEdit();

    expect(store?.mode()).toBe(DetailsMode.View);
    expect(store?.editor()).toEqual({ phaseId: 'a' });
  });

  it('phasesRoute_WithReadPermissionOnly_OpensTheDialogInViewModeWithoutAPencil', async () => {
    const http = setup([Permission.OperatingPhasesRead]);
    const harness = await openPhases(http);
    const store = harness.routeDebugElement?.injector.get(OperatingPhasesPageStore);

    store?.requestUpdate('a');
    await vi.waitFor(() => expect(store?.editor()).not.toBeNull());
    TestBed.tick();

    expect(store?.canWrite()).toBe(false);
    expect(store?.mode()).toBe(DetailsMode.View);
    expect(store?.isEditShown()).toBe(false);
  });

  it('phasesRoute_ComposeOnLoad_OpensInViewMode', async () => {
    const http = setup();

    const harness = await openPhases(http, '/machines/m1/phases?compose=phase:a');
    const store = harness.routeDebugElement?.injector.get(OperatingPhasesPageStore);
    await vi.waitFor(() => expect(store?.mode()).toBe(DetailsMode.View));

    expect(store?.editor()).toEqual({ phaseId: 'a' });
    expect(TestBed.inject(Router).url).toBe('/machines/m1/phases?compose=phase:a');
  });

  it('phasesRoute_UnknownPhaseId_RemovesCompose', async () => {
    const http = setup();
    const navigation = TestBed.inject(PlatformNavigation) as FakePlatformNavigation;

    const harness = await openPhases(http, '/machines/m1/phases?compose=phase:unknown');
    const entryCount = navigation.entries().length;
    const store = harness.routeDebugElement?.injector.get(OperatingPhasesPageStore);
    await vi.waitFor(() => expect(TestBed.inject(Router).url).toBe('/machines/m1/phases'));

    expect(store?.editor()).toBeNull();
    expect(navigation.entries().length).toBe(entryCount);
  });

  it('phasesRoute_MissingMachine_LeadsToTheNotFoundPage', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/machines/m1/phases');
    await vi.waitFor(() => {
      http.expectOne(MACHINE_URL).flush(null, { status: 404, statusText: 'Not Found' });
    });
    await navigation;

    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(NotFoundStub);
  });
});
