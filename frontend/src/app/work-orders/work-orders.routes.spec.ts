import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';

import { AuthStore } from '@shared/security/auth.store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import { BOARD_STATUSES } from '@work-orders/board/board.store.utils';
import { BoardPage } from '@work-orders/board/board-page/board-page';
import { WorkOrdersPage } from '@work-orders/list/work-orders-page/work-orders-page';
import { WORK_ORDERS_ROUTES } from '@work-orders/work-orders.routes';

const SEARCH_URL = '/api/core/v1/work-orders/search';
const SEARCH_BATCH_URL = `${SEARCH_URL}/batch`;
class ResizeObserverStub {
  observe = vi.fn();
  unobserve = vi.fn();
  disconnect = vi.fn();
}

function setup(): HttpTestingController {
  vi.stubGlobal('ResizeObserver', ResizeObserverStub);
  TestBed.configureTestingModule({
    providers: [
      provideRouter([{ path: 'work-orders', children: WORK_ORDERS_ROUTES }], withComponentInputBinding()),
      provideHttpClient(),
      provideHttpClientTesting(),
      provideTranslateService(),
      { provide: AuthStore, useValue: { hasPermission: () => true } },
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

describe('work orders routes', () => {
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
});
