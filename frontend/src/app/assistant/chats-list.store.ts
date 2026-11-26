import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, finalize, pipe, Subject, takeUntil } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

import { CHATS_PAGE_SIZE } from '@assistant/chats-list.store.utils';
import type { ChatListItem } from '@shared/chat/chat';
import { ChatApi } from '@shared/chat/chat-api';
import { withStoreDevtools } from '@shared/devtools/with-store-devtools';
import type { KeysetPosition } from '@shared/grid/requests/grid-request';

interface ListState {
  items: readonly ChatListItem[];
  nextPosition: KeysetPosition | null;
  isLoading: boolean;
  isLoadingNext: boolean;
  hasLoaded: boolean;
}

/** Holds the lazy list of the chats of the user. One keyset page loads at a time. Provide it on the feature route. */
export const ChatsListStore = signalStore(
  withState<ListState>({ items: [], nextPosition: null, isLoading: false, isLoadingNext: false, hasLoaded: false }),
  withProps(() => ({ _api: inject(ChatApi) })),
  withComputed((store) => ({
    hasMore: computed(() => store.nextPosition() !== null),
  })),
  withMethods((store) => {
    const reloaded$ = new Subject<void>();
    const loadNext = rxMethod<void>(
      pipe(
        exhaustMap(() => {
          if (store.hasLoaded() && store.nextPosition() === null) {
            return EMPTY;
          }
          const isFirst = !store.hasLoaded();
          patchState(store, isFirst ? { isLoading: true } : { isLoadingNext: true });
          const after = store.nextPosition() ?? undefined;
          return store._api.search({ limit: CHATS_PAGE_SIZE, filter: {}, after }).pipe(
            takeUntil(reloaded$),
            tap((page) => {
              patchState(store, {
                items: [...store.items(), ...page.items],
                nextPosition: page.nextPosition,
                hasLoaded: true,
              });
            }),
            catchError(() => EMPTY),
            finalize(() => patchState(store, { isLoading: false, isLoadingNext: false })),
          );
        }),
      ),
    );
    return {
      /** Loads the next page, or the first page when none loaded yet. It does nothing after the last page. */
      loadNext,
      /** Loads the list again from the first page. A running page load ends, so its page never joins the new list. */
      reload(): void {
        reloaded$.next();
        patchState(store, { items: [], nextPosition: null, hasLoaded: false });
        loadNext();
      },
    };
  }),
  withHooks({
    onInit(store) {
      store.loadNext();
    },
  }),
  withStoreDevtools('chats-list'),
);
