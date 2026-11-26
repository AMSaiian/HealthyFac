import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';

import { APP_ACTION_HOSTS } from '@app/app-action-hosts';
import { APP_ROUTER_FEATURES } from '@app/app-router';
import { provideActionHosts } from '@shared/actions/action-registry';
import { ActionStore } from '@shared/actions/action.store';
import { provideFakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { provideFakeAuthStore } from '@shared/security/testing/fake-auth-store';
import { CLOCK } from '@shared/time/clock';
import { Permission } from '@shared/security/permissions';
import { Temporal } from '@shared/time/temporal';

import { BOARD_STATUSES } from '@work-orders/board/board-store/board.store.utils';
import { BoardPage } from '@work-orders/board/board-page/board-page';
import { WorkOrdersPage } from '@work-orders/list/work-orders-page/work-orders-page';
import { WORK_ORDERS_ROUTES } from '@work-orders/work-orders.routes';
import { DayScheduleSource } from '@work-orders/work-order';
import { WorkOrderActionType } from '@work-orders/work-order-actions';
import { SchedulePage } from '@work-orders/schedule/schedule-page/schedule-page';
import { SCHEDULE_ROUTES } from '@schedule/schedule.routes';
import { provideFakeRealtimeConnection } from '@shared/realtime/testing/fake-realtime-connection';

const SEARCH_URL = '/api/core/v1/work-orders/search';
const SEARCH_BATCH_URL = `${SEARCH_URL}/batch`;
const COMPANY_SCHEDULE_URL = '/api/core/v1/company/schedule';
const OWN_EMPLOYEE_URL = '/api/core/v1/employees/me';
const OWN_SCHEDULE_URL = '/api/core/v1/employees/e1/schedule';
const TODAY = '2026-01-01';
const OWN_EMPLOYEE = { id: 'e1', firstName: 'Ann', lastName: 'Lee', roleCode: 'worker' };
const OWN_DAY = {
  date: TODAY,
  timeZoneId: 'Europe/Kyiv',
  source: DayScheduleSource.Template,
  blocks: [],
  workOrders: [],
};
class ResizeObserverStub {
  observe = vi.fn();
  unobserve = vi.fn();
  disconnect = vi.fn();
}

function setup(canManage = true): HttpTestingController {
  vi.stubGlobal('ResizeObserver', ResizeObserverStub);
  TestBed.configureTestingModule({
    providers: [
      provideFakeRealtimeConnection(),
      provideRouter(
        [
          { path: 'work-orders', children: WORK_ORDERS_ROUTES },
          { path: '', children: SCHEDULE_ROUTES },
        ],
        ...APP_ROUTER_FEATURES,
      ),
      provideFakePlatformNavigation(),
      provideActionHosts(APP_ACTION_HOSTS),
      provideHttpClient(),
      provideHttpClientTesting(),
      provideTranslateService(),
      provideFakeAuthStore({
        isAuthenticated: () => true,
        hasPermission: (permission: Permission) => canManage || permission !== Permission.WorkOrdersManage,
      }),
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

describe('work orders routes', { tags: ['WorkOrders'] }, () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('workOrdersRoute_OnOpen_ActivatesTheListAndSendsOneSearch', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/work-orders');
    TestBed.tick();

    await vi.waitFor(() => {
      expect(http.match(SEARCH_URL).length).toBeGreaterThan(0);
    });
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(WorkOrdersPage);
  });

  it('boardRoute_OnOpen_ActivatesTheBoardAndSearchesEachColumn', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/work-orders/board');
    TestBed.tick();

    const statuses = new Set<string | undefined>();
    await vi.waitFor(() => {
      TestBed.tick();
      http.match(SEARCH_BATCH_URL).forEach((request) => {
        (request.request.body as { searches: { filter: { statuses: unknown } }[] }).searches.forEach((search) => {
          statuses.add(JSON.stringify(search.filter.statuses));
        });
      });
      BOARD_STATUSES.forEach((status) => {
        expect([...statuses].some((value) => value?.includes(status))).toBe(true);
      });
    });
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(BoardPage);
  });

  it('workOrdersRoute_DialogOverDrawer_KeepsCompose', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/work-orders');
    TestBed.tick();
    await vi.waitFor(() => expect(http.match(SEARCH_URL).length).toBeGreaterThan(0));
    const actions = TestBed.inject(ActionStore);
    const drawer = { type: WorkOrderActionType.Open, payload: { workOrderId: 'w1' } };
    await actions.dispatch(drawer);
    await vi.waitFor(() => expect(actions.active()).toEqual(drawer));

    await actions.dispatchChild({
      type: WorkOrderActionType.ChangeStatus,
      payload: { workOrderId: 'w1', rowVersion: 3 },
    });
    const childType = actions.active()?.type;
    actions.cancel();
    await new Promise((resolve) => setTimeout(resolve));

    expect(childType).toBe(WorkOrderActionType.ChangeStatus);
    expect(actions.active()).toEqual(drawer);
    expect(TestBed.inject(Router).url).toBe('/work-orders?compose=work-order:w1');
  });

  it('scheduleRoute_AsASupervisor_RequestsTheCompanyDayAndTheBacklogOnce', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/work-orders/schedule');
    TestBed.tick();

    await vi.waitFor(() => {
      TestBed.tick();
      const days = http.match((request) => request.url === COMPANY_SCHEDULE_URL);
      const searches = http.match((request) => request.url === SEARCH_URL);
      expect(days.map((request) => request.request.params.get('date'))).toEqual([TODAY]);
      expect(JSON.stringify(searches.map((request) => (request.request.body as { filter: unknown }).filter))).toContain(
        'backlog',
      );
    });
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(SchedulePage);
  });

  it('scheduleRoute_AsAWorker_RequestsTheOwnDayAndNoBacklog', async () => {
    const http = setup(false);
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/work-orders/schedule');
    TestBed.tick();
    await vi.waitFor(() => {
      TestBed.tick();
      http.match(OWN_EMPLOYEE_URL).forEach((request) => request.flush(OWN_EMPLOYEE));
      TestBed.tick();
      const days = http.match((request) => request.url === OWN_SCHEDULE_URL);
      expect(days.map((request) => request.request.params.get('date'))).toEqual([TODAY]);
      days.forEach((request) => request.flush(OWN_DAY));
    });

    http.expectNone(COMPANY_SCHEDULE_URL);
    http.expectNone((request) => request.url === SEARCH_URL);
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(SchedulePage);
  });

  it('companyScheduleRoute_OnOpen_LandsOnTheScheduleTab', async () => {
    setup();
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/company/schedule');
    TestBed.tick();

    expect(TestBed.inject(Router).url).toBe('/work-orders/schedule');
  });
});
