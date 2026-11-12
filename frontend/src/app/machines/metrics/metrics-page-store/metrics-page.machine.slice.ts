import { inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStoreFeature, withMethods, withProps, withState } from '@ngrx/signals';

import { skipErrorToast } from '@shared/http/http-error-key';

import type { Machine } from '@machines/machine';
import { DraftsApi } from '@machines/drafts/drafts-api';

interface MachineState {
  machineId: string | null;
  machine: Machine | null;
  draftAttemptId: string | null;
}

/** The machine of the page, and the attempt whose metric drafts the page stages. */
export function withMetricsPageMachine() {
  return signalStoreFeature(
    withState<MachineState>({ machineId: null, machine: null, draftAttemptId: null }),
    withProps(() => ({ _draftsApi: inject(DraftsApi) })),
    withProps((store) => ({
      drafts: rxResource({
        params: () => {
          const machineId = store.machineId();
          const attemptId = store.draftAttemptId();
          return machineId === null || attemptId === null ? undefined : attemptId;
        },
        stream: ({ params }) => store._draftsApi.listForAttempt(params, skipErrorToast()),
      }),
    })),
    withMethods((store) => ({
      _setMachine(machineId: string, machine: Machine): void {
        patchState(store, { machineId, machine });
      },
      /** Sets the attempt whose pending metric drafts the page stages as proposed items. */
      loadDrafts(attemptId: string | null): void {
        patchState(store, { draftAttemptId: attemptId });
      },
    })),
  );
}
