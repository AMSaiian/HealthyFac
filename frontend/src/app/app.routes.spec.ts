import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';

import { CompanyStore } from '@shared/company/company.store';
import { NETWORK_ERROR_PATH } from '@shared/http/fallback-urls';
import { AuthStore } from '@shared/security/auth-store/auth.store';

import { NetworkErrorPage } from '@fallback/network-error/network-error-page';
import { CompanyFilesPage } from '@files/list/company-files-page/company-files-page';
import { CasesPage } from '@maintenance/list/cases-page/cases-page';
import { MachinesPage } from '@machines/list/machines-page/machines-page';

import { routes } from './app.routes';
import { provideFakeRealtimeConnection } from '@shared/realtime/testing/fake-realtime-connection';

const ROUTE_LOAD_TIMEOUT = 10_000;
const COMPANY_URL = '/api/core/v1/company';
const MACHINES_SEARCH_URL = '/api/core/v1/machines/search';
const MACHINES_LOOKUP_URL = '/api/core/v1/machines/lookup';
const CASES_SEARCH_URL = '/api/insights/v1/cases/search';
const FILES_SEARCH_URL = '/api/document/v1/files/search';
const COMPANY = { name: 'Test Company', timeZoneId: 'Europe/Kyiv' };
const SERVER_ERROR = { status: 500, statusText: 'Server Error' };

function setup(): HttpTestingController {
  TestBed.configureTestingModule({
    providers: [
      provideFakeRealtimeConnection(),
      provideRouter(routes),
      provideHttpClient(),
      provideHttpClientTesting(),
      provideTranslateService(),
      { provide: AuthStore, useValue: { isAuthenticated: () => true, hasPermission: () => true } },
      { provide: MessageService, useValue: { add: vi.fn() } },
      { provide: ConfirmationService, useValue: { confirm: vi.fn() } },
    ],
  });
  return TestBed.inject(HttpTestingController);
}

describe('app routes', { timeout: ROUTE_LOAD_TIMEOUT * 2 }, () => {
  it('networkErrorRoute_IsNotLazy', () => {
    const [first] = routes;

    expect(first.path).toBe(NETWORK_ERROR_PATH);
    expect(first.component).toBe(NetworkErrorPage);
    expect(first.loadComponent).toBeUndefined();
    expect(first.canActivate).toBeUndefined();
  });

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

  it('machinesRoute_AfterCompanyLoaded_ActivatesThePageWithCompanyZone', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();

    const navigation = harness.navigateByUrl('/machines');
    await vi.waitFor(
      () => {
        http.expectOne(COMPANY_URL).flush(COMPANY);
      },
      { timeout: ROUTE_LOAD_TIMEOUT },
    );
    await navigation;
    TestBed.tick();
    await vi.waitFor(
      () => {
        TestBed.tick();
        const requests = http.match((request) => request.url === MACHINES_SEARCH_URL);
        expect(requests.length).toBeGreaterThan(0);
        requests
          .filter((request) => !request.cancelled)
          .forEach((request) => request.flush({ items: [], totalCount: 0 }));
      },
      { timeout: ROUTE_LOAD_TIMEOUT },
    );
    await TestBed.inject(ApplicationRef).whenStable();

    expect(harness.routeDebugElement?.componentInstance instanceof MachinesPage).toBe(true);
    expect(TestBed.inject(CompanyStore).timeZoneId()).toBe('Europe/Kyiv');
    http.verify();
  });

  it('maintenanceRoute_WithTheCasesPermission_ActivatesTheCasesPageAndSearchesTheCases', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();

    const navigation = harness.navigateByUrl('/maintenance/cases');
    await vi.waitFor(
      () => {
        http.expectOne(COMPANY_URL).flush(COMPANY);
      },
      { timeout: ROUTE_LOAD_TIMEOUT },
    );
    await navigation;
    TestBed.tick();
    await vi.waitFor(
      () => {
        TestBed.tick();
        expect(http.match((request) => request.url === CASES_SEARCH_URL).length).toBeGreaterThan(0);
      },
      { timeout: ROUTE_LOAD_TIMEOUT },
    );

    expect(harness.routeDebugElement?.componentInstance instanceof CasesPage).toBe(true);
  });

  it('machinesRoute_AfterCompanyError_ActivatesNoPageAndAsksOnce', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();

    const navigation = harness.navigateByUrl('/machines');
    await vi.waitFor(
      () => {
        http.expectOne(COMPANY_URL).flush(null, SERVER_ERROR);
      },
      { timeout: ROUTE_LOAD_TIMEOUT },
    );
    await navigation;
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(harness.routeDebugElement?.componentInstance instanceof MachinesPage).toBe(false);
    http.expectNone(MACHINES_SEARCH_URL);
    http.expectNone(COMPANY_URL);
    http.verify();
  });

  it('machinesRoute_AfterFailedLoadAndRetry_ActivatesThePage', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();
    const failed = harness.navigateByUrl('/machines');
    await vi.waitFor(
      () => {
        http.expectOne(COMPANY_URL).flush(null, SERVER_ERROR);
      },
      { timeout: ROUTE_LOAD_TIMEOUT },
    );
    await failed;
    TestBed.tick();
    expect(harness.routeDebugElement?.componentInstance instanceof MachinesPage).toBe(false);

    const retried = harness.navigateByUrl('/machines');
    await vi.waitFor(
      () => {
        http.expectOne(COMPANY_URL).flush(COMPANY);
      },
      { timeout: ROUTE_LOAD_TIMEOUT },
    );
    await retried;
    TestBed.tick();
    await vi.waitFor(
      () => {
        TestBed.tick();
        const requests = http.match((request) => request.url === MACHINES_SEARCH_URL);
        expect(requests.length).toBeGreaterThan(0);
        requests
          .filter((request) => !request.cancelled)
          .forEach((request) => request.flush({ items: [], totalCount: 0 }));
      },
      { timeout: ROUTE_LOAD_TIMEOUT },
    );
    await TestBed.inject(ApplicationRef).whenStable();

    expect(harness.routeDebugElement?.componentInstance instanceof MachinesPage).toBe(true);
    expect(TestBed.inject(CompanyStore).timeZoneId()).toBe('Europe/Kyiv');
    http.verify();
  });

  it('filesRoute_AfterCompanyLoaded_ActivatesThePageAndSearchesWithTheCompanyZone', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();

    const navigation = harness.navigateByUrl('/files');
    await vi.waitFor(
      () => {
        http.expectOne(COMPANY_URL).flush(COMPANY);
      },
      { timeout: ROUTE_LOAD_TIMEOUT },
    );
    await navigation;
    TestBed.tick();
    await vi.waitFor(
      () => {
        TestBed.tick();
        const requests = http.match((request) => request.url === FILES_SEARCH_URL);
        expect(requests.length).toBeGreaterThan(0);
        requests
          .filter((request) => !request.cancelled)
          .forEach((request) => request.flush({ items: [], totalCount: 0 }));
        http.expectOne(MACHINES_LOOKUP_URL).flush({ items: [] });
      },
      { timeout: ROUTE_LOAD_TIMEOUT },
    );
    await TestBed.inject(ApplicationRef).whenStable();

    expect(harness.routeDebugElement?.componentInstance instanceof CompanyFilesPage).toBe(true);
    http.verify();
  });
});
