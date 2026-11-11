import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';

import { FALLBACK_ROUTES } from '@fallback/fallback.routes';
import { NotFoundPage } from '@fallback/not-found/not-found-page';
import { AuthStore } from '@shared/security/auth.store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import { CompanyDayPage } from '@schedule/company-day/company-day-page/company-day-page';
import { EmployeeDayPage } from '@schedule/employee-day/employee-day-page/employee-day-page';
import { SCHEDULE_ROUTES } from '@schedule/schedule.routes';

const EMPLOYEE_DAY_URL = '/api/core/v1/employees/e1/schedule';
const COMPANY_DAY_URL = '/api/core/v1/company/schedule';
const TODAY = '2026-01-01';

class ResizeObserverStub {
  observe = vi.fn();
  unobserve = vi.fn();
  disconnect = vi.fn();
}

function setup(hasPermission = true): HttpTestingController {
  vi.stubGlobal('ResizeObserver', ResizeObserverStub);
  TestBed.configureTestingModule({
    providers: [
      provideRouter(
        [{ path: '', children: SCHEDULE_ROUTES }, { path: '', children: [] }, ...FALLBACK_ROUTES],
        withComponentInputBinding(),
      ),
      provideHttpClient(),
      provideHttpClientTesting(),
      provideTranslateService(),
      { provide: AuthStore, useValue: { isAuthenticated: () => true, hasPermission: () => hasPermission } },
      {
        provide: CLOCK,
        useValue: { now: () => Temporal.Instant.from('2026-01-01T10:00:00Z'), timeZoneId: () => 'Europe/Kyiv' },
      },
      { provide: MessageService, useValue: { add: vi.fn() } },
      { provide: ConfirmationService, useValue: { confirm: vi.fn() } },
    ],
  });
  return TestBed.inject(HttpTestingController);
}

describe('schedule routes', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('employeeScheduleRoute_OnOpen_ActivatesThePageAndRequestsTheDayOfToday', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/employees/e1/schedule');
    TestBed.tick();

    await vi.waitFor(() => {
      TestBed.tick();
      const requests = http.match((request) => request.url === EMPLOYEE_DAY_URL);
      expect(requests.map((request) => request.request.params.get('date'))).toContain(TODAY);
    });
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(EmployeeDayPage);
  });

  it('employeeScheduleRoute_WithUnknownEmployee_ShowsTheNotFoundPage', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/employees/e1/schedule');
    TestBed.tick();
    await vi.waitFor(() => {
      TestBed.tick();
      const requests = http.match((request) => request.url === EMPLOYEE_DAY_URL);
      expect(requests.length).toBeGreaterThan(0);
      requests[0].flush(null, { status: HttpStatusCode.NotFound, statusText: 'Not Found' });
    });

    await vi.waitFor(() => {
      TestBed.tick();
      expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(NotFoundPage);
    });
  });

  it('companyScheduleRoute_OnOpen_ActivatesThePageAndRequestsTheDayOfToday', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/company/schedule');
    TestBed.tick();

    await vi.waitFor(() => {
      TestBed.tick();
      const requests = http.match((request) => request.url === COMPANY_DAY_URL);
      expect(requests.map((request) => request.request.params.get('date'))).toContain(TODAY);
    });
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(CompanyDayPage);
  });

  it('companyScheduleRoute_WithoutShiftsManage_RedirectsToHomeWithoutRequests', async () => {
    const http = setup(false);
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/company/schedule');
    TestBed.tick();

    expect(TestBed.inject(Router).url).toBe('/');
    http.expectNone((request) => request.url === COMPANY_DAY_URL);
  });
});
