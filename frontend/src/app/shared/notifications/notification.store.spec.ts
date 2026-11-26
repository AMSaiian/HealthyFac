import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { Subject } from 'rxjs';

import { NotificationStore } from './notification.store';
import { ActionStore } from '@shared/actions/action.store';
import { LanguageStore } from '@shared/i18n/language.store';
import { type RealtimeNotification, RealtimeNotificationKind } from '@shared/realtime/realtime';
import { RealtimeConnection } from '@shared/realtime/realtime-connection';
import { provideFakeAuthStore } from '@shared/security/testing/fake-auth-store';

import { WorkOrderActionType } from '@work-orders/work-order-actions';

const OWN_SUBJECT = 'subject-own';

function notification(id: string, actorSubject: string | null = null): RealtimeNotification {
  return {
    notificationId: id,
    kind: RealtimeNotificationKind.WorkOrderAssigned,
    occurredAt: '2026-11-21T10:00:00Z',
    actorSubject,
    data: { workOrderId: 'w-1', title: 'Pump', plannedStartLocal: '2026-11-21T09:00:00' },
  } as RealtimeNotification;
}

describe('NotificationStore', { tags: ['Realtime'] }, () => {
  let notifications$: Subject<RealtimeNotification>;
  let sessionEnded$: Subject<void>;
  let stop: ReturnType<typeof vi.fn>;
  let logout: ReturnType<typeof vi.fn>;
  let toast: ReturnType<typeof vi.fn>;
  let navigate: ReturnType<typeof vi.fn>;
  let dispatch: ReturnType<typeof vi.fn>;

  function createStore(): InstanceType<typeof NotificationStore> {
    TestBed.configureTestingModule({
      providers: [
        { provide: RealtimeConnection, useValue: { notifications$, sessionEnded$, stop } },
        provideFakeAuthStore({ subject: () => OWN_SUBJECT, logout }),
        { provide: MessageService, useValue: { add: toast } },
        { provide: Router, useValue: { navigate } },
        { provide: ActionStore, useValue: { dispatch } },
        { provide: TranslateService, useValue: { instant: (key: string) => `text:${key}` } },
        { provide: LanguageStore, useValue: { language: signal('en') } },
      ],
    });
    return TestBed.inject(NotificationStore);
  }

  beforeEach(() => {
    notifications$ = new Subject<RealtimeNotification>();
    sessionEnded$ = new Subject<void>();
    stop = vi.fn().mockResolvedValue(undefined);
    logout = vi.fn().mockResolvedValue(undefined);
    toast = vi.fn();
    navigate = vi.fn().mockResolvedValue(true);
    dispatch = vi.fn().mockResolvedValue(undefined);
  });

  it('receive_WithANotification_AddsOneEntryAndShowsOneToast', () => {
    const store = createStore();

    notifications$.next(notification('n-1'));

    expect(store.entries()).toHaveLength(1);
    expect(toast).toHaveBeenCalledTimes(1);
    expect(toast).toHaveBeenCalledWith(expect.objectContaining({ life: 8000, closable: false }));
  });

  it('items_AfterANotification_HoldTheEntryAndItsText', () => {
    const store = createStore();

    notifications$.next(notification('n-1'));

    expect(store.items()).toHaveLength(1);
    expect(store.items()[0].text).toBe('text:NOTIFICATIONS.KINDS.WORK_ORDER_ASSIGNED');
    expect(store.items()[0].entry.notification.notificationId).toBe('n-1');
  });

  it('receive_WithARepeatedId_AddsNothing', () => {
    const store = createStore();

    notifications$.next(notification('n-1'));
    notifications$.next(notification('n-1'));

    expect(store.entries()).toHaveLength(1);
    expect(toast).toHaveBeenCalledTimes(1);
  });

  it('receive_With101Notifications_KeepsTheNewest100FirstOnTop', () => {
    const store = createStore();

    for (let index = 1; index <= 101; index++) {
      notifications$.next(notification(`n-${index}`));
    }

    expect(store.entries()).toHaveLength(100);
    expect(store.entries()[0].notification.notificationId).toBe('n-101');
    expect(store.entries()[99].notification.notificationId).toBe('n-2');
  });

  it('receive_NewNotification_AddsAnUnreadEntry', () => {
    const store = createStore();

    notifications$.next(notification('n-1'));

    expect(store.entries()[0].isRead).toBe(false);
    expect(store.unreadCount()).toBe(1);
  });

  it('openBell_WithUnreadEntries_KeepsTheUnreadCount', () => {
    const store = createStore();
    notifications$.next(notification('n-1'));

    store.openBell();

    expect(store.unreadCount()).toBe(1);
  });

  it('markAsRead_ForOneEntry_LowersTheCountByOneAndKeepsTheEntry', () => {
    const store = createStore();
    notifications$.next(notification('n-1'));
    notifications$.next(notification('n-2'));

    store.markAsRead('n-1');

    expect(store.unreadCount()).toBe(1);
    expect(store.entries()).toHaveLength(2);
    expect(store.entries().find((entry) => entry.notification.notificationId === 'n-1')?.isRead).toBe(true);
  });

  it('markAllAsRead_Always_SetsTheCountToZeroAndKeepsTheEntries', () => {
    const store = createStore();
    notifications$.next(notification('n-1'));
    notifications$.next(notification('n-2'));

    store.markAllAsRead();

    expect(store.unreadCount()).toBe(0);
    expect(store.entries()).toHaveLength(2);
  });

  it('dismiss_ForAnEntry_MarksItAsReadAndLowersTheCount', () => {
    const store = createStore();
    notifications$.next(notification('n-1'));
    notifications$.next(notification('n-2'));

    store.dismiss(store.entries()[0]);

    expect(store.unreadCount()).toBe(1);
    expect(store.entries()[0].isRead).toBe(true);
  });

  it('receive_ThenTheToastTimesOut_KeepsTheEntryUnread', () => {
    const store = createStore();

    notifications$.next(notification('n-1'));

    expect(store.unreadCount()).toBe(1);
  });

  it('jump_ForAnEntry_MarksThatEntryAsReadOnly', () => {
    const store = createStore();
    notifications$.next(notification('n-1'));
    notifications$.next(notification('n-2'));

    store.jump(store.entries()[1]);

    expect(store.entries().map((entry) => entry.isRead)).toEqual([false, true]);
  });

  it('receive_WithTheOwnActorSubject_AddsNothing', () => {
    const store = createStore();

    notifications$.next(notification('n-1', OWN_SUBJECT));
    notifications$.next(notification('n-2', 'subject-other'));

    expect(store.entries().map((entry) => entry.notification.notificationId)).toEqual(['n-2']);
    expect(toast).toHaveBeenCalledTimes(1);
  });

  it('jump_WithARouteTarget_NavigatesAndClosesTheBell', () => {
    const store = createStore();
    store.openBell();
    const draft = {
      notificationId: 'n-9',
      kind: RealtimeNotificationKind.WorkOrderDraftCreated,
      occurredAt: '2026-11-21T10:00:00Z',
      actorSubject: null,
      data: { draftId: 'd-1', title: 'Draft' },
    } as RealtimeNotification;

    store.jump({ notification: draft, isRead: false });

    expect(navigate).toHaveBeenCalledWith(['/work-orders/drafts'], { queryParams: { draftId: 'd-1' } });
    expect(store.isBellOpen()).toBe(false);
  });

  it('jump_WithAnActionTarget_DispatchesTheAction', () => {
    const store = createStore();

    store.jump({ notification: notification('n-1'), isRead: false });

    expect(dispatch).toHaveBeenCalledWith({ type: WorkOrderActionType.Open, payload: { workOrderId: 'w-1' } });
  });

  it('jump_WithoutATarget_OnlyClosesTheBell', () => {
    const store = createStore();
    store.openBell();
    const unassigned = {
      notificationId: 'n-3',
      kind: RealtimeNotificationKind.WorkOrderUnassigned,
      occurredAt: '2026-11-21T10:00:00Z',
      actorSubject: null,
      data: { title: 'Pump' },
    } as RealtimeNotification;

    store.jump({ notification: unassigned, isRead: false });

    expect(navigate).not.toHaveBeenCalled();
    expect(dispatch).not.toHaveBeenCalled();
    expect(store.isBellOpen()).toBe(false);
  });

  it('sessionEnded_Always_StopsTheConnectionAndSignsOut', async () => {
    createStore();

    sessionEnded$.next();
    await vi.waitFor(() => {
      expect(logout).toHaveBeenCalledWith('/');
    });

    expect(stop).toHaveBeenCalledTimes(1);
  });
});
