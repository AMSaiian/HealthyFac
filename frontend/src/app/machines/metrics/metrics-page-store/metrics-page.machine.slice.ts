import { inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStoreFeature, withMethods, withProps, withState } from '@ngrx/signals';

import { skipErrorToast } from '@shared/http/http-error-key';

import type { Machine } from '@machines/machine';
import { draftScopeOf } from '@machines/drafts/draft';
import { DraftsApi } from '@machines/drafts/drafts-api';

interface MachineState {
  machineId: string | null;
  machine: Machine | null;
  draftAttemptId: string | null;
  draftChatId: string | null;
}

/** The machine of the page, and the attempt or the chat whose metric drafts the page stages. */
export function withMetricsPageMachine() {
  return signalStoreFeature(
    withState<MachineState>({ machineId: null, machine: null, draftAttemptId: null, draftChatId: null }),
    withProps(() => ({ _draftsApi: inject(DraftsApi) })),
    withProps((store) => ({
      drafts: rxResource({
        params: () => {
          const machineId = store.machineId();
          const attemptId = store.draftAttemptId();
          const chatId = store.draftChatId();
          if (machineId === null) {
            return undefined;
          }
          return draftScopeOf({
            ...(attemptId === null ? {} : { attemptId }),
            ...(chatId === null ? {} : { chatId }),
          });
        },
        stream: ({ params }) => store._draftsApi.listForScope(params, skipErrorToast()),
      }),
    })),
    withMethods((store) => ({
      _setMachine(machineId: string, machine: Machine): void {
        patchState(store, { machineId, machine });
      },
      /** Sets the attempt or the chat whose pending metric drafts the page stages as proposed items. */
      loadDrafts(attemptId: string | null, chatId: string | null = null): void {
        patchState(store, { draftAttemptId: attemptId, draftChatId: chatId });
      },
    })),
  );
}
