import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';

import type { IngestHistoryActionPayload } from '@files/file-action-payloads';
import { FileActionType } from '@files/file-actions';
import { IngestApi } from '@files/ingest/ingest-api';

interface HistoryState {
  action: AppAction | null;
}

export const IngestHistoryStore = signalStore(
  withState<HistoryState>({ action: null }),
  withProps(() => ({
    _api: inject(IngestApi),
    _actions: inject(ActionStore),
  })),
  withComputed((store) => ({
    fileId: computed(() => (store.action()?.payload as IngestHistoryActionPayload | undefined)?.fileId),
  })),
  withProps((store) => ({
    attempts: rxResource({
      params: () => store.fileId(),
      stream: ({ params }) => store._api.listForFile(params),
    }),
  })),
  withComputed((store) => ({
    /** The attempts of the file, the newest first. */
    rows: computed(() =>
      store.attempts.hasValue()
        ? [...store.attempts.value().items].sort((left, right) => right.queuedAt.localeCompare(left.queuedAt))
        : [],
    ),
  })),
  withMethods((store) => ({
    start(action: AppAction): void {
      patchState(store, { action });
    },
    /** Opens the summary of one attempt. The summary replaces this dialog. */
    openSummary(attemptId: string): void {
      void store._actions.dispatch({ type: FileActionType.IngestSummary, payload: { attemptId } });
    },
    close(): void {
      store._actions.cancel();
    },
  })),
);
