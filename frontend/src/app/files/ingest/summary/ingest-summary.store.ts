import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';

import type { AppAction } from '@shared/actions/app-action';
import { skipErrorToast } from '@shared/http/http-error-key';

import type { IngestSummaryActionPayload } from '@files/file-action-payloads';
import { IngestApi } from '@files/ingest/ingest-api';
import { attemptReasonKey, parseWarning } from '@files/ingest/ingest-attempt.utils';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

function isNotFound(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === HttpStatusCode.NotFound;
}

/**
 * The notices of the ingest summary: why the attempt proposed no drafts, and its warning. The list of drafts is the
 * shared drafts dialog (`MachineDraftsStore`).
 */
export const IngestSummaryStore = signalStore(
  withState<{ action: AppAction | null }>({ action: null }),
  withProps(() => ({ _ingestApi: inject(IngestApi) })),
  withComputed((store) => ({
    attemptId: computed(() => (store.action()?.payload as IngestSummaryActionPayload | undefined)?.attemptId),
  })),
  withProps((store) => ({
    attempt: rxResource({
      params: () => store.attemptId(),
      stream: ({ params }) => store._ingestApi.get(params, skipErrorToast()),
    }),
  })),
  withComputed((store) => ({
    isNotFound: computed(() => isNotFound(store.attempt.error())),
    reasonKey: computed(() => (store.attempt.hasValue() ? attemptReasonKey(store.attempt.value()) : null)),
    warning: computed(() => (store.attempt.hasValue() ? parseWarning(store.attempt.value().warning) : null)),
  })),
  withMethods((store) => ({
    start(action: AppAction): void {
      patchState(store, { action });
    },
  })),
  withStoreDevtools('ingest-summary'),
);
