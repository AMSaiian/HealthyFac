import { HttpStatusCode } from '@angular/common/http';
import { Router } from '@angular/router';
import { computed, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import {
  debounceTime,
  defaultIfEmpty,
  EMPTY,
  exhaustMap,
  filter,
  finalize,
  forkJoin,
  from,
  map,
  mergeMap,
  type Observable,
  of,
  pipe,
  Subject,
  switchMap,
  takeUntil,
  tap,
} from 'rxjs';
import { catchError } from 'rxjs/operators';

import {
  type Chat,
  type ChatDraft,
  type ChatLinkInput,
  ChatLinkKind,
  type ChatMessage,
  ChatMessageState,
  type ChatTurn,
  type ChatTurnEnd,
  type ChatTurnUpdate,
} from '@shared/chat/chat';
import { ChatApi } from '@shared/chat/chat-api';
import {
  appendOlderPage,
  CHAT_MESSAGES_PAGE_SIZE,
  isStaleUpdate,
  mergeNewestPage,
  NEW_CHAT_KEY,
  pendingMessages,
  removeMessageWithReply,
  replaceWithTurn,
  type RunningTurn,
  SEND_TOKEN_SECONDS,
  type SendState,
  STOP_RELOAD_DELAY_MILLISECONDS,
  withoutSend,
} from '@shared/chat/chat.store.utils';
import {
  caseLabel,
  type MentionGroup,
  mentionKinds,
  type MentionOption,
} from '@shared/chat/mention-picker/mention-picker.utils';
import { chatDraftJumpOf } from '@shared/chat/chat-message/chat-message.utils';
import { ActionStore } from '@shared/actions/action.store';
import { withConfirm } from '@shared/confirm/with-confirm';
import { withStoreDevtools } from '@shared/devtools/with-store-devtools';
import { httpErrorKey, isHttpStatus, skipErrorToast } from '@shared/http/http-error-key';
import { LOOKUP_DEBOUNCE_MILLISECONDS } from '@shared/lookups/lookup-options';
import { LookupsApi } from '@shared/lookups/lookups-api';
import { RealtimeConnection } from '@shared/realtime/realtime-connection';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';
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
  /** The send of each chat, by chat ID (`NEW_CHAT_KEY` for a chat that does not exist yet). */
  sends: Readonly<Record<string, SendState>>;
  /** The text of a send that failed in the open chat, so the composer can show it again. */
  unsentText: string | null;
  /** The ID of a chat that a send created, so the page opens it. */
  createdChatId: string | null;
  /** The ID of the chat that the user deleted, so the page leaves it. */
  deletedChatId: string | null;
  /** Counts the ended turns, so a list can reload its order. */
  endedCount: number;
  /** The entities that the `@` search found, grouped by kind. */
  mentionGroups: readonly MentionGroup[];
  isMentionLoading: boolean;
}

const INITIAL_STATE_CLOSED = {
  openChatId: null,
  chat: null,
  messages: [],
  nextBefore: null,
} as const;

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
  sends: {},
  unsentText: null,
  createdChatId: null,
  deletedChatId: null,
  endedCount: 0,
  mentionGroups: [],
  isMentionLoading: false,
};

/** Holds the open chat, its loaded messages, and the streaming turns that the realtime push reports. */
export const ChatStore = signalStore(
  { providedIn: 'root' },
  withState<ChatState>(INITIAL_STATE),
  withProps(() => ({
    _api: inject(ChatApi),
    _connection: inject(RealtimeConnection),
    _auth: inject(AuthStore),
    _actions: inject(ActionStore),
    _router: inject(Router),
    _lookups: inject(LookupsApi),
    _clock: inject(CLOCK),
  })),
  withToasts(),
  withConfirm(),
  withComputed((store) => {
    const currentSend = computed((): SendState | null => store.sends()[store.openChatId() ?? NEW_CHAT_KEY] ?? null);
    const isSending = computed(() => currentSend() !== null && currentSend()?.isStopping === false);
    const runningTurn = computed((): RunningTurn | null => {
      const chatId = store.openChatId();
      if (chatId !== null) {
        return store.running()[chatId] ?? null;
      }
      const knownChatIds = currentSend()?.knownChatIds ?? [];
      const unknown = Object.entries(store.running()).find(([runningChatId]) => !knownChatIds.includes(runningChatId));
      return isSending() ? (unknown?.[1] ?? null) : null;
    });
    return {
      runningTurn,
      /** Tells whether this tab sends the turn of the open chat, so Stop can abort its request. */
      isSending,
      isStopping: computed(() => currentSend()?.isStopping ?? false),
      /** The messages to show: the loaded ones, with the pending message and its empty answer on top while sending. */
      viewMessages: computed(() => {
        const pending = currentSend();
        const runningMessageId = runningTurn()?.messageId;
        const isTurnLoaded =
          store.messages()[0]?.state === ChatMessageState.Streaming ||
          store.messages().some((message) => message.id === runningMessageId);
        return pending === null || isTurnLoaded
          ? store.messages()
          : [...pendingMessages(store.messages(), pending, runningMessageId), ...store.messages()];
      }),
      /** Tells whether an answer is being made in the open chat: a send, a push, or a loaded streaming reply. */
      isRunning: computed(
        () =>
          currentSend() !== null || runningTurn() !== null || store.messages()[0]?.state === ChatMessageState.Streaming,
      ),
    };
  }),
  withMethods((store) => {
    const stop$ = new Subject<string>();
    const stopTimers = new Map<string, ReturnType<typeof setTimeout>>();
    const clearSend = (key: string): void => {
      patchState(store, { sends: withoutSend(store.sends(), key) });
    };
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
              if (
                store.sends()[chatId]?.isStopping === true &&
                !merged.messages.some((message) => message.state === ChatMessageState.Streaming)
              ) {
                clearSend(chatId);
              }
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
    const optionsOf = (rows: readonly { id: string; displayName: string }[], kind: ChatLinkKind): MentionOption[] =>
      rows.map((row) => ({ kind, entityId: row.id, label: row.displayName }));
    const findKind = (kind: ChatLinkKind, search: string): Observable<MentionOption[]> => {
      if (kind === ChatLinkKind.Machine) {
        return store._lookups.machines(search).pipe(map((rows) => optionsOf(rows, kind)));
      }
      if (kind === ChatLinkKind.Employee) {
        return store._lookups.employees(search).pipe(map((rows) => optionsOf(rows, kind)));
      }
      if (kind === ChatLinkKind.WorkOrder) {
        return store._lookups.workOrders(search).pipe(map((rows) => optionsOf(rows, kind)));
      }
      if (kind === ChatLinkKind.File) {
        return store._lookups.files(search).pipe(map((rows) => optionsOf(rows, kind)));
      }
      return store._lookups.machines(search).pipe(
        switchMap((machines) =>
          machines.length === 0
            ? of([])
            : store._lookups.casesOfMachines(machines.map((machine) => machine.id)).pipe(
                map((cases) =>
                  cases.map((found) => ({
                    kind,
                    entityId: found.id,
                    label: caseLabel(
                      machines.find((machine) => machine.id === found.machineId)?.displayName ?? '',
                      Temporal.Instant.from(found.openedAt)
                        .toZonedDateTimeISO(store._clock.timeZoneId())
                        .toPlainDate()
                        .toString(),
                    ),
                  })),
                ),
              ),
        ),
      );
    };
    const searchMentionsCall = rxMethod<string>(
      pipe(
        debounceTime(LOOKUP_DEBOUNCE_MILLISECONDS),
        tap(() => patchState(store, { isMentionLoading: true })),
        switchMap((search) =>
          forkJoin(
            mentionKinds((permission) => store._auth.hasPermission(permission)).map((kind) =>
              findKind(kind, search).pipe(
                map((options): MentionGroup => ({ kind, options })),
                catchError(() => of<MentionGroup>({ kind, options: [] })),
              ),
            ),
          ).pipe(
            defaultIfEmpty([] as MentionGroup[]),
            tap((groups) => patchState(store, { mentionGroups: groups })),
            finalize(() => patchState(store, { isMentionLoading: false })),
          ),
        ),
      ),
    );
    const sendCall = rxMethod<{ key: string; text: string; links: readonly ChatLinkInput[] }>(
      pipe(
        mergeMap(({ key, text, links }) => {
          const chatId = key === NEW_CHAT_KEY ? null : key;
          const body = { text, links };
          return from(store._auth.ensureValidFor(SEND_TOKEN_SECONDS)).pipe(
            switchMap(() =>
              chatId === null
                ? store._api.create(body, skipErrorToast())
                : store._api.send(chatId, body, skipErrorToast()),
            ),
            takeUntil(stop$.pipe(filter((stoppedKey) => stoppedKey === key))),
            tap((turn: ChatTurn) => {
              const { [turn.chat.id]: _finished, ...running } = store.running();
              void _finished;
              const isOpen = store.openChatId() === chatId;
              patchState(store, {
                sends: withoutSend(store.sends(), key),
                running,
                endedCount: store.endedCount() + 1,
                ...(isOpen
                  ? {
                      chat: turn.chat,
                      openChatId: turn.chat.id,
                      messages: replaceWithTurn(chatId === null ? [] : store.messages(), turn),
                      nextBefore: chatId === null ? null : store.nextBefore(),
                      createdChatId: chatId === null ? turn.chat.id : null,
                    }
                  : {}),
              });
            }),
            catchError((error: unknown) => {
              const isOpen = store.openChatId() === chatId;
              patchState(store, {
                sends: withoutSend(store.sends(), key),
                unsentText: isOpen ? text : store.unsentText(),
              });
              if (isHttpStatus(error, HttpStatusCode.Conflict)) {
                store._toastError('CHAT.ERRORS.TURN_IN_PROGRESS');
              } else if (isHttpStatus(error, HttpStatusCode.UnprocessableEntity) && chatId !== null) {
                refresh(chatId);
              } else {
                store._toastError(httpErrorKey(error));
              }
              return EMPTY;
            }),
          );
        }),
      ),
    );
    const deleteMessageCall = rxMethod<ChatMessage>(
      pipe(
        exhaustMap((message) => {
          const chatId = store.openChatId();
          if (chatId === null) {
            return EMPTY;
          }
          return store._api.deleteMessage(chatId, message.id).pipe(
            tap(() => {
              patchState(store, { messages: removeMessageWithReply(store.messages(), message) });
              store._toastSuccess('CHAT.TOASTS.MESSAGE_DELETED');
            }),
            catchError(() => EMPTY),
          );
        }),
      ),
    );
    const deleteChatCall = rxMethod<string>(
      pipe(
        exhaustMap((chatId) =>
          store._api.delete(chatId).pipe(
            tap(() => {
              patchState(store, {
                ...INITIAL_STATE_CLOSED,
                sends: withoutSend(store.sends(), chatId),
                deletedChatId: chatId,
              });
              store._toastSuccess('CHAT.TOASTS.CHAT_DELETED');
            }),
            catchError(() => EMPTY),
          ),
        ),
      ),
    );
    return {
      /** Opens a chat and loads its header and its newest messages. `null` closes the open chat. */
      openChat(chatId: string | null): void {
        if (chatId === store.openChatId()) {
          return;
        }
        patchState(store, { createdChatId: null, deletedChatId: null, unsentText: null });
        load(chatId);
      },
      /** Opens the review of a draft of the open chat: a route, or an action over the page. The drawer stays open. */
      jumpToDraft(draft: ChatDraft): void {
        const chatId = store.openChatId();
        const target = chatId === null ? null : chatDraftJumpOf(draft, chatId);
        if (target === null) {
          return;
        }
        if (target.type === 'route') {
          void store._router.navigate([...target.commands], { queryParams: target.queryParams });
          return;
        }
        void store._actions.dispatch(target.action);
      },
      /** Searches the entities that a mention can name for the typed text, one group per permitted kind. */
      searchMentions(search: string): void {
        searchMentionsCall(search);
      },
      /** Sends a message to the open chat, or creates a chat when none is open. */
      send(text: string, links: readonly ChatLinkInput[]): void {
        const key = store.openChatId() ?? NEW_CHAT_KEY;
        if (store.sends()[key] !== undefined) {
          return;
        }
        const knownChatIds =
          key === NEW_CHAT_KEY
            ? [
                ...Object.keys(store.running()),
                ...Object.keys(store.sends()).filter((sendKey) => sendKey !== NEW_CHAT_KEY),
              ]
            : [];
        patchState(store, {
          sends: { ...store.sends(), [key]: { text, links, isStopping: false, knownChatIds } },
          unsentText: null,
        });
        sendCall({ key, text, links });
      },
      /** Stops the answer that this tab sends in the open chat: aborts the request, then waits for the end push. */
      stop(): void {
        const key = store.openChatId() ?? NEW_CHAT_KEY;
        const send = store.sends()[key];
        if (send === undefined || send.isStopping) {
          return;
        }
        stop$.next(key);
        patchState(store, { sends: { ...store.sends(), [key]: { ...send, isStopping: true } } });
        stopTimers.set(
          key,
          setTimeout(() => {
            stopTimers.delete(key);
            if (store.sends()[key]?.isStopping !== true) {
              return;
            }
            if (key !== NEW_CHAT_KEY && store.openChatId() === key) {
              refresh(key);
              return;
            }
            clearSend(key);
          }, STOP_RELOAD_DELAY_MILLISECONDS),
        );
      },
      /** Asks to delete a user message with its reply. */
      requestDeleteMessage(message: ChatMessage): void {
        store._confirmDestructive({
          headerKey: 'CHAT.DELETE.CONFIRM_MESSAGE_HEADER',
          messageKey: 'CHAT.DELETE.CONFIRM_MESSAGE',
          acceptKey: 'ACTIONS.DELETE',
          onAccept: () => {
            deleteMessageCall(message);
          },
        });
      },
      /** Asks to delete the open chat. */
      requestDeleteChat(): void {
        const chatId = store.openChatId();
        if (chatId === null) {
          return;
        }
        store._confirmDestructive({
          headerKey: 'CHAT.DELETE.CONFIRM_CHAT_HEADER',
          messageKey: 'CHAT.DELETE.CONFIRM_CHAT',
          acceptKey: 'ACTIONS.DELETE',
          onAccept: () => {
            deleteChatCall(chatId);
          },
        });
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
        patchState(store, { running: rest, endedCount: store.endedCount() + 1 });
        const timer = stopTimers.get(end.chatId);
        if (timer !== undefined) {
          clearTimeout(timer);
          stopTimers.delete(end.chatId);
        }
        const sendOfNewChat = store.sends()[NEW_CHAT_KEY];
        if (
          sendOfNewChat?.isStopping === true &&
          store.openChatId() === null &&
          !sendOfNewChat.knownChatIds.includes(end.chatId)
        ) {
          const timerOfNew = stopTimers.get(NEW_CHAT_KEY);
          if (timerOfNew !== undefined) {
            clearTimeout(timerOfNew);
            stopTimers.delete(NEW_CHAT_KEY);
          }
          clearSend(NEW_CHAT_KEY);
          patchState(store, { createdChatId: end.chatId });
          return;
        }
        if (store.sends()[end.chatId]?.isStopping === true) {
          clearSend(end.chatId);
        }
        if (store.openChatId() === end.chatId) {
          refresh(end.chatId);
        }
      },
      openDrawer(): void {
        patchState(store, { isDrawerOpen: true });
      },
      toggleDrawer(): void {
        patchState(store, { isDrawerOpen: !store.isDrawerOpen() });
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
