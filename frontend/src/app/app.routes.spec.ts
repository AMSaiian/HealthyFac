import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';

import { CompanyStore } from '@shared/company/company.store';
import { AuthStore } from '@shared/security/auth.store';

import { MachinesPage } from '@machines/list/machines-page/machines-page';

import { routes } from './app.routes';

const COMPANY_URL = '/api/core/v1/company';
const MACHINES_SEARCH_URL = '/api/core/v1/machines/search';
const COMPANY = { name: 'Test Company', timeZoneId: 'Europe/Kyiv' };
const SERVER_ERROR = { status: 500, statusText: 'Server Error' };

function setup(): HttpTestingController {
  TestBed.configureTestingModule({
    providers: [
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

describe('app routes', () => {
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
    await vi.waitFor(() => {
      http.expectOne(COMPANY_URL).flush(COMPANY);
    });
    await navigation;
    TestBed.tick();
    await vi.waitFor(() => {
      TestBed.tick();
      const requests = http.match((request) => request.url === MACHINES_SEARCH_URL);
      expect(requests.length).toBeGreaterThan(0);
      requests
        .filter((request) => !request.cancelled)
        .forEach((request) => request.flush({ items: [], totalCount: 0 }));
    });
    await TestBed.inject(ApplicationRef).whenStable();

    expect(harness.routeDebugElement?.componentInstance instanceof MachinesPage).toBe(true);
    expect(TestBed.inject(CompanyStore).timeZoneId()).toBe('Europe/Kyiv');
    http.verify();
  });

  it('machinesRoute_AfterCompanyError_ActivatesNoPageAndAsksOnce', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();

    const navigation = harness.navigateByUrl('/machines');
    await vi.waitFor(() => {
      http.expectOne(COMPANY_URL).flush(null, SERVER_ERROR);
    });
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
    await vi.waitFor(() => {
      http.expectOne(COMPANY_URL).flush(null, SERVER_ERROR);
    });
    await failed;
    TestBed.tick();
    expect(harness.routeDebugElement?.componentInstance instanceof MachinesPage).toBe(false);

    const retried = harness.navigateByUrl('/machines');
    await vi.waitFor(() => {
      http.expectOne(COMPANY_URL).flush(COMPANY);
    });
    await retried;
    TestBed.tick();
    await vi.waitFor(() => {
      TestBed.tick();
      const requests = http.match((request) => request.url === MACHINES_SEARCH_URL);
      expect(requests.length).toBeGreaterThan(0);
      requests
        .filter((request) => !request.cancelled)
        .forEach((request) => request.flush({ items: [], totalCount: 0 }));
    });
    await TestBed.inject(ApplicationRef).whenStable();

    expect(harness.routeDebugElement?.componentInstance instanceof MachinesPage).toBe(true);
    expect(TestBed.inject(CompanyStore).timeZoneId()).toBe('Europe/Kyiv');
    http.verify();
  });
});
