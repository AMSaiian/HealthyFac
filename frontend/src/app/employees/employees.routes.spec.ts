import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { PlatformNavigation } from '@angular/common';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';

import { APP_ROUTER_FEATURES } from '@app/app-router';
import { EmployeeRole } from '@shared/employee/employee';
import type { FakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { provideFakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { provideFakeAuthStore } from '@shared/security/testing/fake-auth-store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import { EMPLOYEES_ROUTES } from '@employees/employees.routes';
import { EmployeesPage } from '@employees/list/employees-page/employees-page';
import { ShiftsPage } from '@employees/shifts/shifts-page/shifts-page';
import { ShiftsStore } from '@employees/shifts/shifts-store/shifts.store';

const EMPLOYEES_SEARCH_URL = '/api/core/v1/employees/search';
const TEMPLATES_URL = '/api/core/v1/employees/e1/shift-templates';
const OVERRIDES_URL = '/api/core/v1/employees/e1/shift-overrides';

class ResizeObserverStub {
  observe = vi.fn();
  unobserve = vi.fn();
  disconnect = vi.fn();
}

function setup(hasPermission: boolean): HttpTestingController {
  vi.stubGlobal('ResizeObserver', ResizeObserverStub);
  TestBed.configureTestingModule({
    providers: [
      provideRouter(
        [
          { path: 'employees', children: EMPLOYEES_ROUTES },
          { path: '', children: [] },
        ],
        ...APP_ROUTER_FEATURES,
      ),
      provideFakePlatformNavigation(),
      provideHttpClient(),
      provideHttpClientTesting(),
      provideTranslateService(),
      provideFakeAuthStore({ hasPermission: () => hasPermission }),
      {
        provide: CLOCK,
        useValue: { now: () => Temporal.Instant.from('2026-01-01T00:00:00Z'), timeZoneId: () => 'Europe/Kyiv' },
      },
      { provide: MessageService, useValue: { add: vi.fn() } },
      { provide: ConfirmationService, useValue: { confirm: vi.fn() } },
    ],
  });
  return TestBed.inject(HttpTestingController);
}

async function openShifts(http: HttpTestingController, url: string): Promise<RouterTestingHarness> {
  const harness = await RouterTestingHarness.create();
  await harness.navigateByUrl(url);
  TestBed.tick();
  await vi.waitFor(() => {
    TestBed.tick();
    const requests = http.match((request) => request.url === TEMPLATES_URL || request.url === OVERRIDES_URL);
    expect(requests.length).toBe(2);
    requests.forEach((request) => request.flush([]));
  });
  return harness;
}

describe('employees routes', { tags: ['Companies', 'Shifts'] }, () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('employeesRoute_WithPermission_ActivatesTheListAndSearchesEmployees', async () => {
    const http = setup(true);
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/employees');
    TestBed.tick();

    await vi.waitFor(() => {
      expect(http.match(EMPLOYEES_SEARCH_URL).length).toBeGreaterThan(0);
    });
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(EmployeesPage);
  });

  it('employeesRoute_WithoutEmployeesRead_RedirectsToHomeWithoutRequests', async () => {
    const http = setup(false);
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/employees');
    TestBed.tick();

    expect(TestBed.inject(Router).url).toBe('/');
    http.expectNone(EMPLOYEES_SEARCH_URL);
  });

  it('employeesRoute_AfterLeavingAndReturning_SearchesWithTheInitialFilter', async () => {
    const http = setup(true);
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/employees');
    TestBed.tick();
    await vi.waitFor(() => {
      expect(http.match(EMPLOYEES_SEARCH_URL).length).toBeGreaterThan(0);
    });

    const page = harness.routeDebugElement?.componentInstance as EmployeesPage;
    Reflect.get(page, 'store').setFilter({ roles: [EmployeeRole.Worker] });
    TestBed.tick();
    await vi.waitFor(() => {
      expect(http.match(EMPLOYEES_SEARCH_URL).length).toBeGreaterThan(0);
    });

    await harness.navigateByUrl('/');
    TestBed.tick();
    await harness.navigateByUrl('/employees');
    TestBed.tick();

    let returned: TestRequest[] = [];
    await vi.waitFor(() => {
      TestBed.tick();
      returned = http.match(EMPLOYEES_SEARCH_URL);
      expect(returned.length).toBeGreaterThan(0);
    });
    expect(returned.at(-1)?.request.body).not.toEqual(
      expect.objectContaining({ filter: expect.objectContaining({ roles: { values: ['worker'] } }) }),
    );
  });

  it('shiftsRoute_WithEmployee_ActivatesThePageAndRequestsVersionsAndOverrides', async () => {
    const http = setup(true);
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/employees/e1/shifts');
    TestBed.tick();

    await vi.waitFor(() => {
      TestBed.tick();
      expect(http.match((request) => request.url === TEMPLATES_URL).length).toBeGreaterThan(0);
    });
    await vi.waitFor(() => {
      TestBed.tick();
      expect(http.match((request) => request.url === OVERRIDES_URL).length).toBeGreaterThan(0);
    });
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(ShiftsPage);
  });

  it('shiftsRoute_ComposeOnLoad_OpensTheVersionDialog', async () => {
    const http = setup(true);

    const harness = await openShifts(http, '/employees/e1/shifts?compose=shift-version');
    const store = harness.routeDebugElement?.injector.get(ShiftsStore);
    await new Promise((resolve) => setTimeout(resolve));

    expect(store?.dialog()).toEqual({ kind: 'version', version: null });
    expect(TestBed.inject(Router).url).toBe('/employees/e1/shifts?compose=shift-version');
  });

  it('shiftsRoute_UnknownOverrideId_RemovesCompose', async () => {
    const http = setup(true);
    const navigation = TestBed.inject(PlatformNavigation) as FakePlatformNavigation;

    const harness = await openShifts(http, '/employees/e1/shifts?compose=shift-override:unknown');
    const entryCount = navigation.entries().length;
    const store = harness.routeDebugElement?.injector.get(ShiftsStore);
    await vi.waitFor(() => expect(TestBed.inject(Router).url).toBe('/employees/e1/shifts'));

    expect(store?.dialog()).toBeNull();
    expect(navigation.entries().length).toBe(entryCount);
  });
});
