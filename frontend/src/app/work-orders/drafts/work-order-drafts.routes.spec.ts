import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';

import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';

import { DraftState } from '@work-orders/drafts/work-order-draft';
import { WorkOrderDraftsGridStore } from '@work-orders/drafts/work-order-drafts-grid.store';
import { WorkOrderDraftsPage } from '@work-orders/drafts/work-order-drafts-page/work-order-drafts-page';
import { WorkOrderCategory, WorkOrderPriority } from '@work-orders/work-order';
import { WORK_ORDERS_ROUTES } from '@work-orders/work-orders.routes';

const SEARCH_URL = '/api/core/v1/work-order-drafts/search';
const DRAFT_URL = '/api/core/v1/work-order-drafts/d1';

const DRAFT = {
  id: 'd1',
  machineId: 'm1',
  machineName: 'Lathe',
  isMachineDeleted: false,
  caseId: 'c1',
  runId: 'r1',
  title: 'Replace the bearing',
  description: null,
  priority: WorkOrderPriority.High,
  category: WorkOrderCategory.Repair,
  estimateMinutes: 60,
  state: DraftState.Pending,
  appliedWorkOrderId: null,
  proposedAt: '2026-01-01T00:00:00Z',
  rowVersion: 7,
};

class ResizeObserverStub {
  observe = vi.fn();
  unobserve = vi.fn();
  disconnect = vi.fn();
}

function setup(permissions: readonly string[]): {
  http: HttpTestingController;
  messages: { add: ReturnType<typeof vi.fn> };
  confirm: ReturnType<typeof vi.fn>;
} {
  vi.stubGlobal('ResizeObserver', ResizeObserverStub);
  const messages = { add: vi.fn() };
  const confirm = vi.fn((options: { accept: () => void }) => options.accept());
  TestBed.configureTestingModule({
    providers: [
      provideRouter(
        [
          { path: 'work-orders', children: WORK_ORDERS_ROUTES },
          { path: '', children: [] },
        ],
        withComponentInputBinding(),
      ),
      provideHttpClient(),
      provideHttpClientTesting(),
      provideTranslateService(),
      { provide: AuthStore, useValue: { hasPermission: (permission: string) => permissions.includes(permission) } },
      { provide: MessageService, useValue: messages },
      { provide: ConfirmationService, useValue: { confirm } },
    ],
  });
  return { http: TestBed.inject(HttpTestingController), messages, confirm };
}

async function openDrafts(http: HttpTestingController): Promise<InstanceType<typeof WorkOrderDraftsGridStore>> {
  const harness = await RouterTestingHarness.create();
  await harness.navigateByUrl('/work-orders/drafts');
  await vi.waitFor(() => {
    TestBed.tick();
    const searches = http.match(SEARCH_URL);
    expect(searches.length).toBeGreaterThan(0);
    searches
      .filter((request) => !request.cancelled)
      .forEach((request) => request.flush({ items: [DRAFT], totalCount: 1 }));
  });
  expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(WorkOrderDraftsPage);
  return harness.routeDebugElement?.injector.get(WorkOrderDraftsGridStore) as InstanceType<
    typeof WorkOrderDraftsGridStore
  >;
}

describe('work order drafts route', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('draftsRoute_OnOpen_SearchesThePendingDraftsNewestFirst', async () => {
    const { http } = setup([Permission.DraftsRead]);
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/work-orders/drafts');
    let body: unknown;
    await vi.waitFor(() => {
      TestBed.tick();
      const searches = http.match(SEARCH_URL);
      expect(searches.length).toBeGreaterThan(0);
      body = searches[0].request.body;
    });

    expect(body).toMatchObject({
      sort: { column: 'proposedAt', direction: 'desc' },
      filter: { states: { values: ['pending'] } },
    });
  });

  it('draftsRoute_WithDraftIdQuery_LoadsTheDraftAndOpensTheApplyDialog', async () => {
    const { http } = setup([Permission.DraftsRead, Permission.DraftsWrite, Permission.WorkOrdersWrite]);
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/work-orders/drafts?draftId=d1');
    await vi.waitFor(() => {
      TestBed.tick();
      http.match(SEARCH_URL).forEach((request) => request.flush({ items: [], totalCount: 0 }));
      http.expectOne({ method: 'GET', url: DRAFT_URL }).flush(DRAFT);
    });

    const store = harness.routeDebugElement?.injector.get(WorkOrderDraftsGridStore) as InstanceType<
      typeof WorkOrderDraftsGridStore
    >;
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(WorkOrderDraftsPage);
    expect(store.editedDraft()?.id).toBe('d1');
  });

  it.each([
    ['a draft that is not pending', 200, { ...DRAFT, state: DraftState.Applied }, 'DRAFTS.CONFLICTS.DRAFT_NOT_PENDING'],
    ['an answer 422', 422, null, 'DRAFTS.CONFLICTS.DRAFT_NOT_PENDING'],
    ['an unknown draft', 404, null, 'ERRORS.HTTP.NOT_FOUND'],
  ])('draftsRoute_WithDraftIdQueryFor%s_ShowsTheToastAndKeepsTheGrid', async (_name, status, body, key) => {
    const { http, messages } = setup([Permission.DraftsRead, Permission.DraftsWrite, Permission.WorkOrdersWrite]);
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/work-orders/drafts?draftId=d1');
    await vi.waitFor(() => {
      TestBed.tick();
      http.match(SEARCH_URL).forEach((request) => request.flush({ items: [DRAFT], totalCount: 1 }));
      http.expectOne({ method: 'GET', url: DRAFT_URL }).flush(body, { status, statusText: 'Answer' });
    });

    const store = harness.routeDebugElement?.injector.get(WorkOrderDraftsGridStore) as InstanceType<
      typeof WorkOrderDraftsGridStore
    >;
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error', detail: key }));
    expect(store.editedDraft()).toBeNull();
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(WorkOrderDraftsPage);
  });

  it('draftsRoute_WithoutTheDraftsPermission_GoesHome', async () => {
    const { http } = setup([Permission.WorkOrdersRead]);
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/work-orders/drafts');

    http.expectNone(SEARCH_URL);
    expect(harness.routeDebugElement?.componentInstance).not.toBeInstanceOf(WorkOrderDraftsPage);
  });

  it('draftsRoute_UserWithoutTheWorkOrdersWritePermission_CannotApplyButCanDismiss', async () => {
    const { http } = setup([Permission.DraftsRead, Permission.DraftsWrite]);

    const store = await openDrafts(http);

    expect(store.canApply()).toBe(false);
    expect(store.canDismiss()).toBe(true);
  });

  it('draftsRoute_ApplyWithEditedValues_SendsThemWithTheRowVersionAndReloadsTheGrid', async () => {
    const { http, messages } = setup([Permission.DraftsRead, Permission.DraftsWrite, Permission.WorkOrdersWrite]);
    const store = await openDrafts(http);
    store.requestApply('d1');

    store.apply({
      rowVersion: 7,
      title: 'Replace the bearing now',
      description: null,
      priority: WorkOrderPriority.Critical,
      category: WorkOrderCategory.Repair,
      estimateMinutes: 90,
    });

    await vi.waitFor(() => {
      const request = http.expectOne(`${DRAFT_URL}/apply`);
      expect(request.request.body).toMatchObject({
        rowVersion: 7,
        title: 'Replace the bearing now',
        estimateMinutes: 90,
      });
      request.flush({});
    });
    await vi.waitFor(() => {
      TestBed.tick();
      expect(http.match(SEARCH_URL).length).toBeGreaterThan(0);
    });
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
    expect(store.editedDraft()).toBeNull();
  });

  it.each([
    [422, 'DRAFTS.CONFLICTS.DRAFT_NOT_PENDING'],
    [409, 'WORK_ORDER_DRAFTS.CONFLICTS.CHANGED'],
  ])('draftsRoute_ApplyAnswers%i_ShowsTheConflictTextAndReloads', async (status, key) => {
    const { http, messages } = setup([Permission.DraftsRead, Permission.DraftsWrite, Permission.WorkOrdersWrite]);
    const store = await openDrafts(http);
    store.requestApply('d1');

    store.apply({
      rowVersion: 7,
      title: 'x',
      description: null,
      priority: WorkOrderPriority.Low,
      category: WorkOrderCategory.Other,
      estimateMinutes: 5,
    });

    await vi.waitFor(() => {
      http.expectOne(`${DRAFT_URL}/apply`).flush(null, { status, statusText: 'Conflict' });
    });
    await vi.waitFor(() => {
      TestBed.tick();
      expect(http.match(SEARCH_URL).length).toBeGreaterThan(0);
    });
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error', detail: key }));
    expect(store.editedDraft()).toBeNull();
  });

  it('draftsRoute_Dismiss_SendsTheRowVersionAndReloadsTheGrid', async () => {
    const { http, confirm } = setup([Permission.DraftsRead, Permission.DraftsWrite]);
    const store = await openDrafts(http);

    store.requestDismiss(DRAFT as never);

    expect(confirm).toHaveBeenCalled();
    await vi.waitFor(() => {
      const request = http.expectOne(`${DRAFT_URL}/dismiss`);
      expect(request.request.body).toEqual({ rowVersion: 7 });
      request.flush(null);
    });
    await vi.waitFor(() => {
      TestBed.tick();
      expect(http.match(SEARCH_URL).length).toBeGreaterThan(0);
    });
  });
});
