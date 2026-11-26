import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PlatformNavigation } from '@angular/common';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';

import { APP_ROUTER_FEATURES } from '@app/app-router';
import type { FakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { provideFakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { HistoryStore } from '@shared/history/history.store';
import { HOME_RETURN_TARGET } from '@shared/history/return-target';
import { MACHINES_RETURN_TARGET } from '@machines/machines-return-target';
import { provideFakeAuthStore } from '@shared/security/testing/fake-auth-store';
import { Permission } from '@shared/security/permissions';

import { CasesPage } from '@maintenance/list/cases-page/cases-page';
import { CasePage } from '@maintenance/detail/case-page/case-page';
import { CasePageStore } from '@maintenance/detail/case-page-store/case-page.store';
import { MAINTENANCE_ROUTES } from '@maintenance/maintenance.routes';
import { CompanyStore } from '@shared/company/company.store';
import { provideFakeRealtimeConnection } from '@shared/realtime/testing/fake-realtime-connection';

const SEARCH_URL = '/api/insights/v1/cases/search';
const CASE_URL = '/api/insights/v1/cases/c1';
const LOOKUP_URL = '/api/core/v1/machines/lookup';
const UNPROCESSABLE = { status: 422, statusText: 'Unprocessable Entity' };

const CASE = {
  id: 'c1',
  machineId: 'm1',
  state: 'open',
  highestOpenSeverity: 'alarm',
  signalCount: 1,
  lastRunState: 'failed',
  isAiUnavailable: true,
  openedAt: '2026-01-01T00:00:00Z',
  closedAt: null,
  closeReason: null,
  outcome: { kind: 'watched', workOrderDraftId: null, workOrderId: null, title: null, reason: 'Stable.' },
};
const SIGNALS_URL = `${CASE_URL}/signals/search`;
const RUNS_URL = `${CASE_URL}/runs/search`;
const EMPTY_PAGE = { items: [], totalCount: 0 };

@Component({ template: '', selector: 'hf-not-found-stub', host: { class: 'contents' } })
class NotFoundStub {}

@Component({ template: '', selector: 'hf-network-error-stub', host: { class: 'contents' } })
class NetworkErrorStub {}

function setup(permissions: readonly string[]): {
  http: HttpTestingController;
  messages: { add: ReturnType<typeof vi.fn> };
} {
  const messages = { add: vi.fn() };
  TestBed.configureTestingModule({
    providers: [
      provideFakeRealtimeConnection(),
      provideRouter(
        [
          { path: 'maintenance', children: MAINTENANCE_ROUTES },
          { path: 'not-found', component: NotFoundStub },
          { path: 'network-error', component: NetworkErrorStub },
          { path: 'machines/:id/signals', component: NotFoundStub },
        ],
        ...APP_ROUTER_FEATURES,
      ),
      provideFakePlatformNavigation(),
      provideHttpClient(),
      provideHttpClientTesting(),
      provideTranslateService(),
      provideFakeAuthStore({
        permissions,
      }),
      { provide: CompanyStore, useValue: { timeZoneId: () => 'Europe/Kyiv' } },
      { provide: MessageService, useValue: messages },
    ],
  });
  return { http: TestBed.inject(HttpTestingController), messages };
}

async function flushChildren(http: HttpTestingController): Promise<void> {
  await vi.waitFor(() => {
    http.expectOne(SIGNALS_URL).flush(EMPTY_PAGE);
    http.expectOne(RUNS_URL).flush(EMPTY_PAGE);
  });
}

async function openCase(
  http: HttpTestingController,
  isMachineDeleted = false,
): Promise<InstanceType<typeof CasePageStore>> {
  const harness = await RouterTestingHarness.create();
  const navigation = harness.navigateByUrl('/maintenance/cases/c1');
  await vi.waitFor(() => {
    http.expectOne(CASE_URL).flush(CASE);
  });
  await navigation;
  TestBed.tick();
  await flushChildren(http);
  await vi.waitFor(() => {
    const lookup = http.expectOne(LOOKUP_URL);
    expect(lookup.request.body).toEqual({ ids: ['m1'] });
    lookup.flush({ items: [{ id: 'm1', displayName: 'Lathe', isDeleted: isMachineDeleted }] });
  });
  await TestBed.inject(ApplicationRef).whenStable();
  expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(CasePage);
  return harness.routeDebugElement?.injector.get(CasePageStore) as InstanceType<typeof CasePageStore>;
}

describe('maintenance routes', { tags: ['Cases'] }, () => {
  beforeEach(() => {
    vi.stubGlobal(
      'ResizeObserver',
      class {
        observe = vi.fn();
        unobserve = vi.fn();
        disconnect = vi.fn();
      },
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('caseToSignals_Back_ReturnsToTheCase', async () => {
    const { http } = setup([Permission.CasesRead]);
    await openCase(http);
    const router = TestBed.inject(Router);
    const navigation = TestBed.inject(PlatformNavigation) as FakePlatformNavigation;
    await router.navigateByUrl('/machines/m1/signals');
    const entryCount = navigation.entries().length;

    const back = TestBed.inject(HistoryStore).back(MACHINES_RETURN_TARGET);
    await vi.waitFor(() => {
      http.expectOne(CASE_URL).flush(CASE);
    });
    await back;

    expect(router.url).toBe('/maintenance/cases/c1');
    expect(navigation.currentEntry.index).toBe(entryCount - 2);
    expect(navigation.entries().length).toBe(entryCount);
  });

  it('caseRoute_404FromALink_BackReturnsToTheSource', async () => {
    const { http } = setup([Permission.CasesRead]);
    const history = TestBed.inject(HistoryStore);
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/maintenance/cases');
    await vi.waitFor(() => {
      TestBed.tick();
      http.match(SEARCH_URL).forEach((request) => request.flush(EMPTY_PAGE));
      expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(CasesPage);
    });
    const navigation = TestBed.inject(PlatformNavigation) as FakePlatformNavigation;

    const opening = harness.navigateByUrl('/maintenance/cases/c1');
    await vi.waitFor(() => {
      http.expectOne(CASE_URL).flush(null, { status: 404, statusText: 'Not Found' });
    });
    await opening;

    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(NotFoundStub);
    expect(navigation.currentEntry.url).toContain('/maintenance/cases/c1');

    const entryCount = navigation.entries().length;

    await history.back(HOME_RETURN_TARGET);
    expect(TestBed.inject(Router).url).toBe('/maintenance/cases');
    expect(navigation.entries().length).toBe(entryCount);
    expect(navigation.currentEntry.index).toBe(entryCount - 2);
  });

  it('caseRoute_Status0_ShowsTheNetworkPage', async () => {
    const { http, messages } = setup([Permission.CasesRead]);
    const harness = await RouterTestingHarness.create();

    const opening = harness.navigateByUrl('/maintenance/cases/c1');
    await vi.waitFor(() => {
      http.expectOne(CASE_URL).error(new ProgressEvent('error'), { status: 0 });
    });
    await opening;

    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(NetworkErrorStub);
    expect(messages.add).not.toHaveBeenCalled();
    expect((TestBed.inject(PlatformNavigation) as FakePlatformNavigation).currentEntry.url).toContain(
      '/maintenance/cases/c1',
    );
  });

  it('casesRoute_AfterOpening_SearchesTheCasesNewestFirstAndLoadsTheMachineNames', async () => {
    const { http } = setup([Permission.CasesRead]);
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/maintenance/cases');
    TestBed.tick();

    let bodies: unknown[] = [];
    await vi.waitFor(() => {
      TestBed.tick();
      const searches = http.match(SEARCH_URL);
      expect(searches.length).toBeGreaterThan(0);
      bodies = searches.map((search) => search.request.body);
    });
    expect(bodies[0]).toMatchObject({ offset: 0, sort: { column: 'openedAt', direction: 'desc' } });
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(CasesPage);
  });

  it('casesRoute_PageWithManyMachines_AsksTheLookupOnceWithTheDistinctMachineIdsOfThePage', async () => {
    const { http } = setup([Permission.CasesRead]);
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/maintenance/cases');
    const items = ['m1', 'm2', 'm1', 'm3'].map((machineId, index) => ({ ...CASE, id: `c${index}`, machineId }));

    await vi.waitFor(() => {
      TestBed.tick();
      const searches = http.match(SEARCH_URL);
      expect(searches.length).toBeGreaterThan(0);
      searches.filter((request) => !request.cancelled).forEach((request) => request.flush({ items, totalCount: 4 }));
    });
    let bodies: unknown[] = [];
    await vi.waitFor(() => {
      TestBed.tick();
      const lookups = http.match(LOOKUP_URL);
      expect(lookups.length).toBeGreaterThan(0);
      bodies = lookups.map((request) => request.request.body);
    });

    expect(bodies).toEqual([{ ids: ['m1', 'm2', 'm3'] }]);
  });

  it('casesRoute_EmptyPage_SendsNoLookup', async () => {
    const { http } = setup([Permission.CasesRead]);
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/maintenance/cases');

    await vi.waitFor(() => {
      TestBed.tick();
      const searches = http.match(SEARCH_URL);
      expect(searches.length).toBeGreaterThan(0);
      searches
        .filter((request) => !request.cancelled)
        .forEach((request) => request.flush({ items: [], totalCount: 0 }));
    });
    TestBed.tick();

    http.expectNone(LOOKUP_URL);
  });

  it('caseRoute_UnknownCaseId_LeadsToTheNotFoundPage', async () => {
    const { http } = setup([Permission.CasesRead]);
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/maintenance/cases/c1');

    await vi.waitFor(() => {
      http.expectOne(CASE_URL).flush(null, { status: 404, statusText: 'Not Found' });
    });
    await navigation;

    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(NotFoundStub);
  });

  it('caseRoute_OpenCaseWithoutWritePermission_OffersNoAction', async () => {
    const { http } = setup([Permission.CasesRead]);

    const store = await openCase(http);

    expect(store.canAct()).toBe(false);
    expect(store.machineLabel()).toEqual({ name: 'Lathe', isDeleted: false });
  });

  it('caseRoute_CaseOfADeletedMachine_MarksTheMachineLabelAsDeleted', async () => {
    const { http } = setup([Permission.CasesRead]);

    const store = await openCase(http, true);

    expect(store.machineLabel()).toEqual({ name: 'Lathe', isDeleted: true });
  });

  it('caseRoute_OpenCaseWithWritePermission_OffersTheActions', async () => {
    const { http } = setup([Permission.CasesRead, Permission.CasesWrite]);

    const store = await openCase(http);

    expect(store.canAct()).toBe(true);
  });

  it('caseRoute_ClosedCaseWithWritePermission_OffersNoAction', async () => {
    const { http } = setup([Permission.CasesRead, Permission.CasesWrite]);
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/maintenance/cases/c1');
    await vi.waitFor(() => {
      http.expectOne(CASE_URL).flush({ ...CASE, state: 'closed' });
    });
    await navigation;

    const store = harness.routeDebugElement?.injector.get(CasePageStore) as InstanceType<typeof CasePageStore>;

    expect(store.canAct()).toBe(false);
  });

  it('caseRoute_RefreshClick_ReloadsTheHeaderAndBothListsOnceAndIgnoresAClickDuringTheCooldown', async () => {
    const { http } = setup([Permission.CasesRead]);
    const store = await openCase(http);

    store.refresh();
    await vi.waitFor(() => {
      http.expectOne(CASE_URL).flush(CASE);
    });
    await flushChildren(http);

    store.refresh();
    await TestBed.inject(ApplicationRef).whenStable();

    http.expectNone(CASE_URL);
    http.expectNone(SIGNALS_URL);
    http.expectNone(RUNS_URL);
    expect(store.isRefreshCoolingDown()).toBe(true);
    expect(store.refreshSecondsLeft()).toBe(5);
  });

  it('caseRoute_PageChange_AsksTheSignalsOfThatPage', async () => {
    const { http } = setup([Permission.CasesRead]);
    const store = await openCase(http);

    store.setSignalsPage(25, 25);

    await vi.waitFor(() => {
      const request = http.expectOne(SIGNALS_URL);
      expect(request.request.body).toEqual({ offset: 25, limit: 25 });
      request.flush(EMPTY_PAGE);
    });
  });

  it('caseRoute_OpenRun_LoadsItsToolCallsOnceAndKeepsThem', async () => {
    const { http } = setup([Permission.CasesRead]);
    const store = await openCase(http);

    store.openRun('r1');
    store.openRun('r1');

    await vi.waitFor(() => {
      http.expectOne(`${CASE_URL}/runs/r1/tool-calls`).flush({ toolCalls: [{ sequence: 1 }] });
    });
    store.openRun('r1');
    http.expectNone(`${CASE_URL}/runs/r1/tool-calls`);
    expect(store.toolCallsByRun()['r1']).toHaveLength(1);
  });

  it('caseRoute_RunAgain_PostsTheRunToastsAndReloadsTheCase', async () => {
    const { http, messages } = setup([Permission.CasesRead, Permission.CasesWrite]);
    const store = await openCase(http);

    store.runAgain();

    await vi.waitFor(() => {
      const request = http.expectOne(`${CASE_URL}/runs`);
      expect(request.request.method).toBe('POST');
      request.flush(null);
    });
    await vi.waitFor(() => {
      http.expectOne(CASE_URL).flush({ ...CASE, isAiUnavailable: false });
    });
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
    expect(store.detail()?.isAiUnavailable).toBe(false);
  });

  it('caseRoute_CloseWithAReason_PostsTheReasonAndClosesTheDialog', async () => {
    const { http } = setup([Permission.CasesRead, Permission.CasesWrite]);
    const store = await openCase(http);
    store.requestClose();

    store.close('Fixed by the technician');

    await vi.waitFor(() => {
      const request = http.expectOne(`${CASE_URL}/close`);
      expect(request.request.body).toEqual({ reason: 'Fixed by the technician' });
      request.flush(null);
    });
    await vi.waitFor(() => {
      http.expectOne(CASE_URL).flush({ ...CASE, state: 'closed' });
    });
    expect(store.isCloseOpen()).toBe(false);
  });

  it('caseRoute_RunAgainOnACaseThatIsNotOpen_ShowsAnErrorToastAndReloads', async () => {
    const { http, messages } = setup([Permission.CasesRead, Permission.CasesWrite]);
    const store = await openCase(http);

    store.runAgain();

    await vi.waitFor(() => {
      http.expectOne(`${CASE_URL}/runs`).flush(null, UNPROCESSABLE);
    });
    await vi.waitFor(() => {
      http.expectOne(CASE_URL).flush({ ...CASE, state: 'closed' });
    });
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
    expect(store.canAct()).toBe(false);
  });
});
