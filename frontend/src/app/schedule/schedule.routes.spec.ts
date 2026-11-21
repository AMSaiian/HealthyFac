import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PlatformNavigation } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';

import { FALLBACK_ROUTES } from '@fallback/fallback.routes';
import { NotFoundPage } from '@fallback/not-found/not-found-page';
import type { FakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { provideFakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import type { DaySchedule } from '@work-orders/work-order';
import { CompanyDayPage } from '@schedule/company-day/company-day-page/company-day-page';
import { EmployeeDayStore } from '@schedule/employee-day/employee-day.store';
import { EmployeeDayPage } from '@schedule/employee-day/employee-day-page/employee-day-page';
import { SCHEDULE_ROUTES } from '@schedule/schedule.routes';

const EMPLOYEE_DAY_URL = '/api/core/v1/employees/e1/schedule';
const COMPANY_DAY_URL = '/api/core/v1/company/schedule';
const TODAY = '2026-01-01';
const NEXT_DAY = '2026-01-02';
const DAY: DaySchedule = { date: TODAY, timeZoneId: 'Europe/Kyiv', source: 'template', blocks: [], workOrders: [] };

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
      provideFakePlatformNavigation(),
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

  it('employeeDayRoute_FirstLoad_RequestsTheDayOnce', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();

    const navigation = harness.navigateByUrl('/employees/e1/schedule');
    await vi.waitFor(() => {
      const request = http.expectOne((candidate) => candidate.url === EMPLOYEE_DAY_URL);
      expect(request.request.params.get('date')).toBe(TODAY);
      request.flush(DAY);
    });
    await navigation;
    TestBed.tick();
    TestBed.tick();

    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(EmployeeDayPage);
    http.expectNone((request) => request.url === EMPLOYEE_DAY_URL);
  });

  it('employeeDayRoute_DateChangeFails_KeepsThePage', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/employees/e1/schedule');
    await vi.waitFor(() => {
      http.expectOne((request) => request.url === EMPLOYEE_DAY_URL).flush(DAY);
    });
    await navigation;
    TestBed.tick();
    const store = harness.routeDebugElement?.injector.get(EmployeeDayStore) as InstanceType<typeof EmployeeDayStore>;

    store.nextDay();
    TestBed.tick();
    await vi.waitFor(() => {
      TestBed.tick();
      const request = http.expectOne((candidate) => candidate.url === EMPLOYEE_DAY_URL);
      expect(request.request.params.get('date')).toBe(NEXT_DAY);
      request.flush(null, { status: HttpStatusCode.InternalServerError, statusText: 'Server Error' });
    });
    TestBed.tick();

    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(EmployeeDayPage);
    expect(store.hasFailed()).toBe(true);
    expect(TestBed.inject(Router).url).toBe('/employees/e1/schedule');
  });

  it('employeeScheduleRoute_WithUnknownEmployee_ShowsTheNotFoundPageAndKeepsTheUrl', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();

    const navigation = harness.navigateByUrl('/employees/e1/schedule');
    await vi.waitFor(() => {
      http
        .expectOne((request) => request.url === EMPLOYEE_DAY_URL)
        .flush(null, { status: HttpStatusCode.NotFound, statusText: 'Not Found' });
    });
    await navigation;

    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(NotFoundPage);
    expect((TestBed.inject(PlatformNavigation) as FakePlatformNavigation).currentEntry.url).toContain(
      '/employees/e1/schedule',
    );
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
