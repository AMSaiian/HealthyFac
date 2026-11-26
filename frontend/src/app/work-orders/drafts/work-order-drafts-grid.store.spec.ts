import { ApplicationRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { of } from 'rxjs';

import { provideFakeAuthStore } from '@shared/security/testing/fake-auth-store';
import { RealtimeTopic } from '@shared/realtime/realtime';
import {
  createFakeRealtimeConnection,
  provideFakeRealtimeConnection,
} from '@shared/realtime/testing/fake-realtime-connection';

import { WorkOrderDraftsApi } from '@work-orders/drafts/work-order-drafts-api';
import { WorkOrderDraftsGridStore } from './work-order-drafts-grid.store';

describe('WorkOrderDraftsGridStore', { tags: ['WorkOrders', 'Drafts'] }, () => {
  const realtime = createFakeRealtimeConnection();
  const search = vi.fn();

  beforeEach(() => {
    search.mockReset().mockReturnValue(of({ items: [], totalCount: 0 }));
    TestBed.configureTestingModule({
      providers: [
        provideFakeRealtimeConnection(realtime),
        provideTranslateService(),
        WorkOrderDraftsGridStore,
        { provide: WorkOrderDraftsApi, useValue: { search } },
        provideFakeAuthStore({ hasPermission: () => signal(true)() }),
        { provide: MessageService, useValue: { add: vi.fn() } },
        { provide: ConfirmationService, useValue: { confirm: vi.fn() } },
      ],
    });
  });

  afterEach(() => vi.useRealTimers());

  it('hint_OfTheWorkOrdersTopic_ReloadsTheDraftsOncePerBurst', async () => {
    TestBed.inject(WorkOrderDraftsGridStore);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    const before = search.mock.calls.length;
    vi.useFakeTimers();

    realtime.hints$.next({ topic: RealtimeTopic.WorkOrders, workOrderId: 'a' });
    realtime.hints$.next({ topic: RealtimeTopic.WorkOrders, workOrderId: 'b' });
    await vi.advanceTimersByTimeAsync(1000);
    vi.useRealTimers();
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(search.mock.calls.length - before).toBe(1);
  });

  it('hint_OfAnotherTopic_DoesNotReloadTheDrafts', async () => {
    TestBed.inject(WorkOrderDraftsGridStore);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    const before = search.mock.calls.length;
    vi.useFakeTimers();

    realtime.hints$.next({ topic: RealtimeTopic.Machines });
    await vi.advanceTimersByTimeAsync(2000);
    vi.useRealTimers();
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(search.mock.calls.length - before).toBe(0);
  });
});
