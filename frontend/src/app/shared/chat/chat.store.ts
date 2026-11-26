import { HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, finalize, forkJoin, pipe, Subject, switchMap, takeUntil, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import type { Chat, ChatMessage, ChatTurnEnd, ChatTurnUpdate } from '@shared/chat/chat';
import { ChatApi } from '@shared/chat/chat-api';
import {
  appendOlderPage,
  CHAT_MESSAGES_PAGE_SIZE,
  isStaleUpdate,
  mergeNewestPage,
  type RunningTurn,
} from '@shared/chat/chat.store.utils';
import { withStoreDevtools } from '@shared/devtools/with-store-devtools';
import { httpErrorKey, isHttpStatus, skipErrorToast } from '@shared/http/http-error-key';
import { RealtimeConnection } from '@shared/realtime/realtime-connection';
import { withToasts } from '@shared/toasts/with-toasts';

interface ChatState {
  openChatId: string | null;
  chat: Chat | null;
  /** The loaded messages, the newest first. */
  messages: readonly ChatMessage[];
  /** The `sequence` that loads the next older page, or `null` when the oldest page is loaded. */
  nextBefore: number | null;
  isLoading: boolean;
  isLoadingOlder: boolean;
  isNotFound: boolean;
  /** The streaming turn of each chat, by chat ID. */
  running: Readonly<Record<string, RunningTurn>>;
  isDrawerOpen: boolean;
}

const INITIAL_STATE: ChatState = {
  openChatId: null,
  chat: null,
  messages: [],
  nextBefore: null,
  isLoading: false,
  isLoadingOlder: false,
  isNotFound: false,
  running: {},
  isDrawerOpen: false,
};

/** Holds the open chat, its loaded messages, and the streaming turns that the realtime push reports. */
export const ChatStore = signalStore(
  { providedIn: 'root' },
  withState<ChatState>(INITIAL_STATE),
  withProps(() => ({
    _api: inject(ChatApi),
    _connection: inject(RealtimeConnection),
  })),
  withToasts(),
  withMethods((store) => {
    const switched$ = new Subject<void>();
    const load = rxMethod<string | null>(
      pipe(
        tap((chatId) => {
          switched$.next();
          patchState(store, {
            openChatId: chatId,
            chat: null,
            messages: [],
            nextBefore: null,
            isNotFound: false,
            isLoading: chatId !== null,
          });
        }),
        switchMap((chatId) => {
          if (chatId === null) {
            return EMPTY;
          }
          return forkJoin({
            chat: store._api.get(chatId, skipErrorToast()),
            page: store._api.messages(chatId, CHAT_MESSAGES_PAGE_SIZE, undefined, skipErrorToast()),
          }).pipe(
            tap(({ chat, page }) => {
              patchState(store, { chat, messages: page.items, nextBefore: page.nextBefore });
            }),
            catchError((error: unknown) => {
              if (isHttpStatus(error, HttpStatusCode.NotFound)) {
                patchState(store, { isNotFound: true });
              } else {
                store._toastError(httpErrorKey(error));
              }
              return EMPTY;
            }),
            finalize(() => {
              if (store.openChatId() === chatId) {
                patchState(store, { isLoading: false });
              }
            }),
          );
        }),
      ),
    );
    const refresh = rxMethod<string>(
      pipe(
        switchMap((chatId) =>
          forkJoin({
            chat: store._api.get(chatId, skipErrorToast()),
            page: store._api.messages(chatId, CHAT_MESSAGES_PAGE_SIZE, undefined, skipErrorToast()),
          }).pipe(
            tap(({ chat, page }) => {
              if (store.openChatId() !== chatId) {
                return;
              }
              const merged = mergeNewestPage(store.messages(), page, store.nextBefore());
              patchState(store, { chat, ...merged });
            }),
            catchError(() => EMPTY),
          ),
        ),
      ),
    );
    const loadOlderPage = rxMethod<void>(
      pipe(
        exhaustMap(() => {
          const chatId = store.openChatId();
          const before = store.nextBefore();
          if (chatId === null || before === null) {
            return EMPTY;
          }
          patchState(store, { isLoadingOlder: true });
          return store._api.messages(chatId, CHAT_MESSAGES_PAGE_SIZE, before).pipe(
            takeUntil(switched$),
            tap((page) => {
              patchState(store, { messages: appendOlderPage(store.messages(), page), nextBefore: page.nextBefore });
            }),
            catchError(() => EMPTY),
            finalize(() => patchState(store, { isLoadingOlder: false })),
          );
        }),
      ),
    );
    return {
      /** Opens a chat and loads its header and its newest messages. `null` closes the open chat. */
      openChat(chatId: string | null): void {
        load(chatId);
      },
      /** Loads the next older page of the open chat. It does nothing after the oldest page. */
      loadOlder(): void {
        loadOlderPage();
      },
      /** Applies a push of a streaming turn. A push that is not newer than the stored one changes nothing. */
      applyUpdate(update: ChatTurnUpdate): void {
        if (isStaleUpdate(store.running()[update.chatId], update)) {
          return;
        }
        patchState(store, {
          running: {
            ...store.running(),
            [update.chatId]: {
              messageId: update.messageId,
              text: update.text,
              sequence: update.sequence,
              activity: update.activity,
            },
          },
        });
      },
      /** Ends the streaming turn of a chat and reloads the open chat, because its messages changed. */
      applyEnd(end: ChatTurnEnd): void {
        const rest = Object.fromEntries(Object.entries(store.running()).filter(([chatId]) => chatId !== end.chatId));
        patchState(store, { running: rest });
        if (store.openChatId() === end.chatId) {
          refresh(end.chatId);
        }
      },
      openDrawer(): void {
        patchState(store, { isDrawerOpen: true });
      },
      closeDrawer(): void {
        patchState(store, { isDrawerOpen: false });
      },
    };
  }),
  withHooks({
    onInit(store) {
      store._connection.chatUpdates$.pipe(takeUntilDestroyed()).subscribe((update) => {
        store.applyUpdate(update);
      });
      store._connection.chatEnds$.pipe(takeUntilDestroyed()).subscribe((end) => {
        store.applyEnd(end);
      });
    },
  }),
  withStoreDevtools('chat'),
);
