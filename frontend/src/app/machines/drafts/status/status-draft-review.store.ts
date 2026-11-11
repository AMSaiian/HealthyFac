import { HttpContext, HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { EMPTY, exhaustMap, finalize, type Observable, of, pipe, tap } from 'rxjs';
import { catchError, concatMap, map } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { httpErrorMessageKey } from '@shared/http/http-error-message';
import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';

import type { StatusDraftReviewActionPayload } from '@machines/drafts/draft-action-payloads';
import { DraftActionType } from '@machines/drafts/draft-actions';
import { DraftsApi } from '@machines/drafts/drafts-api';
import {
  codeSearchBody,
  findStatusByCode,
  findStatusDraft,
  mergeStatus,
  type StatusCompareField,
  type StatusTaken,
  toCreateValue,
  toStatusCompareRows,
} from '@machines/drafts/status/status-draft-review.store.utils';
import type { StatusDefinitionFormValue } from '@machines/statuses/drawer/status-definition-form/status-definition-form-schema';
import { StatusDefinitionsApi } from '@machines/statuses/status-definitions-api';

interface ReviewState {
  action: AppAction | null;
  taken: StatusTaken;
  isSaving: boolean;
}

export const StatusDraftReviewStore = signalStore(
  withState<ReviewState>({ action: null, taken: {}, isSaving: false }),
  withProps(() => ({
    _draftsApi: inject(DraftsApi),
    _statusesApi: inject(StatusDefinitionsApi),
    _actions: inject(ActionStore),
    _messages: inject(MessageService),
    _translate: inject(TranslateService),
  })),
  withComputed((store) => ({
    payload: computed(() => store.action()?.payload as StatusDraftReviewActionPayload | undefined),
  })),
  withProps((store) => ({
    drafts: rxResource({
      params: () => store.payload()?.attemptId,
      stream: ({ params }) => store._draftsApi.listForAttempt(params),
    }),
  })),
  withComputed((store) => ({
    draft: computed(() => {
      const payload = store.payload();
      return store.drafts.hasValue() && payload !== undefined
        ? findStatusDraft(store.drafts.value(), payload.draftId)
        : null;
    }),
  })),
  withProps((store) => ({
    matches: rxResource({
      params: () => {
        const code = store.draft()?.code;
        const machineId = store.payload()?.machineId;
        return code == null || machineId === undefined ? undefined : { machineId, code };
      },
      stream: ({ params }) => store._statusesApi.search(params.machineId, codeSearchBody(params.code)),
    }),
  })),
  withComputed((store) => {
    const existing = computed(() => {
      const code = store.draft()?.code;
      return code == null || !store.matches.hasValue() ? null : findStatusByCode(store.matches.value().items, code);
    });
    return { existing };
  }),
  withComputed((store) => {
    const isSearchPending = computed(() => store.draft()?.code != null && store.matches.status() === 'idle');
    const isLoading = computed(
      () => store.payload() === undefined || store.drafts.isLoading() || store.matches.isLoading() || isSearchPending(),
    );
    const mode = computed((): 'loading' | 'create' | 'update' | 'missing' => {
      if (isLoading()) {
        return 'loading';
      }
      if (store.draft() === null) {
        return 'missing';
      }
      return store.existing() === null ? 'create' : 'update';
    });
    const rows = computed(() => {
      const draft = store.draft();
      const existing = store.existing();
      return draft === null || existing === null ? [] : toStatusCompareRows(draft, existing, store.taken());
    });
    return { isLoading, mode, rows };
  }),
  withComputed((store) => ({
    createValue: computed((): StatusDefinitionFormValue | null => {
      const draft = store.draft();
      return draft === null ? null : toCreateValue(draft);
    }),
    updateValue: computed((): StatusDefinitionFormValue | null => {
      const draft = store.draft();
      const existing = store.existing();
      return draft === null || existing === null ? null : mergeStatus(existing, draft, store.rows());
    }),
  })),
  withComputed((store) => ({
    /** The reason that **Save** is off in update mode: no field is taken. */
    updateDisabledReason: computed((): string | null =>
      store.rows().some((row) => row.isTaken) ? null : 'DRAFTS.STATUS.NO_CHANGES',
    ),
  })),
  withMethods((store) => {
    const showError = (detailKey: string): void => {
      store._messages.add({
        severity: 'error',
        summary: store._translate.instant('ERRORS.SUMMARY'),
        detail: store._translate.instant(detailKey),
      });
    };

    /** Marks the draft applied. A failed mark shows a toast and leaves the draft pending. */
    const mark$ = (draftId: string): Observable<void> =>
      store._draftsApi.markApplied([draftId], { context: new HttpContext().set(SKIP_ERROR_TOAST, true) }).pipe(
        catchError(() => {
          showError('DRAFTS.STATUS.MARK_FAILED');
          return of(undefined);
        }),
      );

    interface SaveRequest {
      readonly value: StatusDefinitionFormValue;
    }

    const save = rxMethod<SaveRequest>(
      pipe(
        tap(() => patchState(store, { isSaving: true })),
        exhaustMap(({ value }) => {
          const payload = store.payload();
          const existing = store.existing();
          if (payload === undefined) {
            patchState(store, { isSaving: false });
            return EMPTY;
          }
          const body = { code: value.code.trim(), severity: value.severity, description: value.description };
          const options = { context: new HttpContext().set(SKIP_ERROR_TOAST, true) };
          const write$: Observable<unknown> =
            existing === null
              ? store._statusesApi.create(payload.machineId, body, options)
              : store._statusesApi.update(payload.machineId, existing.id, body, options);
          return write$.pipe(
            concatMap(() => mark$(payload.draftId)),
            map(() => undefined),
            tap(() => {
              store._messages.add({
                severity: 'success',
                summary: store._translate.instant('FILES.TOASTS.SUCCESS'),
                detail: store._translate.instant(existing === null ? 'DRAFTS.STATUS.CREATED' : 'DRAFTS.STATUS.UPDATED'),
              });
              store._actions.complete(DraftActionType.Changed);
            }),
            catchError((error: unknown) => {
              const status = error instanceof HttpErrorResponse ? error.status : 0;
              showError(status === HttpStatusCode.Conflict ? 'DRAFTS.STATUS.CODE_TAKEN' : httpErrorMessageKey(status));
              return EMPTY;
            }),
            finalize(() => patchState(store, { isSaving: false })),
          );
        }),
      ),
    );

    return {
      start(action: AppAction): void {
        patchState(store, { action, taken: {} });
      },
      take(field: StatusCompareField, isTaken: boolean): void {
        patchState(store, (state) => ({ taken: { ...state.taken, [field]: isTaken } }));
      },
      /** Saves the form value of a new status. */
      saveCreate(value: StatusDefinitionFormValue): void {
        save({ value });
      },
      /** Saves the merged value of an existing status. */
      saveUpdate(): void {
        const value = store.updateValue();
        if (value !== null && store.updateDisabledReason() === null) {
          save({ value });
        }
      },
      cancel(): void {
        store._actions.cancel();
      },
    };
  }),
);
