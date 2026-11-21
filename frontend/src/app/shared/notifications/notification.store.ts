import { computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { pipe, tap } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import { LanguageStore } from '@shared/i18n/language.store';
import { RealtimeConnection } from '@shared/realtime/realtime-connection';
import type { RealtimeNotification } from '@shared/realtime/realtime';
import { AuthStore } from '@shared/security/auth-store/auth.store';

import {
  addEntry,
  hasNotification,
  jumpOf,
  NOTIFICATION_TOAST_LIFE_MILLISECONDS,
  type NotificationEntry,
  type NotificationItem,
  textKeyOf,
  textParamsOf,
} from './notification.store.utils';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

function textOf(translate: TranslateService, entry: NotificationEntry, language: string): string {
  const context = { translate: (key: string) => translate.instant(key), language };
  return translate.instant(textKeyOf(entry.notification.kind), textParamsOf(entry.notification, context));
}

export const NotificationStore = signalStore(
  { providedIn: 'root' },
  withState({ entries: [] as readonly NotificationEntry[], isBellOpen: false }),
  withProps(() => ({
    _connection: inject(RealtimeConnection),
    _auth: inject(AuthStore),
    _messages: inject(MessageService),
    _router: inject(Router),
    _actions: inject(ActionStore),
    _translate: inject(TranslateService),
    _language: inject(LanguageStore),
  })),
  withComputed((store) => ({
    unreadCount: computed(() => store.entries().filter((entry) => !entry.isRead).length),
    /** The entries with their text. The text follows the language of the app. */
    items: computed((): readonly NotificationItem[] => {
      const language = store._language.language();
      return store.entries().map((entry) => ({ entry, text: textOf(store._translate, entry, language) }));
    }),
  })),
  withMethods((store) => {
    const receive = (notification: RealtimeNotification): void => {
      const isRepeat = hasNotification(store.entries(), notification.notificationId);
      const isOwn = notification.actorSubject !== null && notification.actorSubject === store._auth.subject();
      if (isRepeat || isOwn) {
        return;
      }
      const entries = addEntry(store.entries(), notification);
      patchState(store, { entries });
      store._messages.add({
        severity: 'info',
        life: NOTIFICATION_TOAST_LIFE_MILLISECONDS,
        closable: false,
        data: { entry: entries[0], text: textOf(store._translate, entries[0], store._language.language()) },
      });
    };

    const markAsRead = (notificationId: string): void => {
      patchState(store, {
        entries: store
          .entries()
          .map((entry) => (entry.notification.notificationId === notificationId ? { ...entry, isRead: true } : entry)),
      });
    };

    return {
      markAsRead,
      /** The user closed the toast of the entry. The life timeout never calls it, so a timed out entry stays unread. */
      dismiss(entry: NotificationEntry): void {
        markAsRead(entry.notification.notificationId);
      },
      markAllAsRead(): void {
        patchState(store, { entries: store.entries().map((entry) => ({ ...entry, isRead: true })) });
      },
      openBell(): void {
        patchState(store, { isBellOpen: true });
      },
      closeBell(): void {
        patchState(store, { isBellOpen: false });
      },
      /** Marks the entry as read, runs its jump, and closes the bell. An entry without a jump only closes it. */
      jump(entry: NotificationEntry): void {
        patchState(store, { isBellOpen: false });
        markAsRead(entry.notification.notificationId);
        const target = jumpOf(entry.notification);
        if (target === null) {
          return;
        }
        if (target.type === 'route') {
          void store._router.navigate([...target.commands], { queryParams: target.queryParams });
          return;
        }
        void store._actions.dispatch(target.action);
      },
      _receive: rxMethod<RealtimeNotification>(pipe(tap(receive))),
      _endSession: rxMethod<void>(
        pipe(
          tap(() => {
            void store._connection.stop().then(() => store._auth.logout('/'));
          }),
        ),
      ),
    };
  }),
  withHooks({
    onInit(store) {
      store._receive(store._connection.notifications$);
      store._endSession(store._connection.sessionEnded$);
    },
  }),
  withStoreDevtools('notification'),
);
