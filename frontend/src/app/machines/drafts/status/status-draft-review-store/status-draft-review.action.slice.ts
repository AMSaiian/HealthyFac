import { HttpStatusCode } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStoreFeature, type, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, finalize, type Observable, of, pipe, tap } from 'rxjs';
import { catchError, concatMap, map } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { httpErrorKey, skipErrorToast } from '@shared/http/http-error-key';

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
} from '@machines/drafts/status/status-draft-review-store/status-draft-review.store.utils';
import type { StatusDefinitionFormValue } from '@machines/statuses/drawer/status-definition-form/status-definition-form-schema';
import { StatusDefinitionsApi } from '@machines/statuses/status-definitions-api';

interface ActionState {
  action: AppAction | null;
  taken: StatusTaken;
  isSaving: boolean;
}

interface SaveRequest {
  readonly value: StatusDefinitionFormValue;
}

/**
 * The open review: the action, the fields that the user takes, the save in progress, and the save that the review
 * runs. The save saves the new status or the merged status, marks the draft applied, and tells the page.
 */
export function withStatusDraftAction() {
  return signalStoreFeature(
    {
      methods: type<{ _toastSuccess: (key: string) => void; _toastError: (key: string) => void }>(),
    },
    withState<ActionState>({ action: null, taken: {}, isSaving: false }),
    withComputed((store) => ({
      payload: computed(() => store.action()?.payload as StatusDraftReviewActionPayload | undefined),
    })),
    withProps((store) => {
      const draftsApi = inject(DraftsApi);
      return {
        drafts: rxResource({
          params: () => store.payload()?.attemptId,
          stream: ({ params }) => draftsApi.listForAttempt(params),
        }),
      };
    }),
    withComputed((store) => ({
      draft: computed(() => {
        const payload = store.payload();
        return store.drafts.hasValue() && payload !== undefined
          ? findStatusDraft(store.drafts.value(), payload.draftId)
          : null;
      }),
    })),
    withProps((store) => {
      const statusesApi = inject(StatusDefinitionsApi);
      return {
        matches: rxResource({
          params: () => {
            const code = store.draft()?.code;
            const machineId = store.payload()?.machineId;
            return code == null || machineId === undefined ? undefined : { machineId, code };
          },
          stream: ({ params }) => statusesApi.search(params.machineId, codeSearchBody(params.code)),
        }),
      };
    }),
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
        () =>
          store.payload() === undefined || store.drafts.isLoading() || store.matches.isLoading() || isSearchPending(),
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
      /** The reason that **Save** is off in update mode: no field is taken. */
      updateDisabledReason: computed((): string | null =>
        store.rows().some((row) => row.isTaken) ? null : 'DRAFTS.STATUS.NO_CHANGES',
      ),
    })),
    withMethods((store) => ({
      start(action: AppAction): void {
        patchState(store, { action, taken: {} });
      },
      take(field: StatusCompareField, isTaken: boolean): void {
        patchState(store, (state) => ({ taken: { ...state.taken, [field]: isTaken } }));
      },
      _setSaving(isSaving: boolean): void {
        patchState(store, { isSaving });
      },
    })),
    withMethods((store) => {
      const draftsApi = inject(DraftsApi);
      const statusesApi = inject(StatusDefinitionsApi);
      const actions = inject(ActionStore);

      /** Marks the draft applied. A failed mark shows a toast and leaves the draft pending. */
      const mark$ = (draftId: string): Observable<void> =>
        draftsApi.markApplied([draftId], skipErrorToast()).pipe(
          catchError(() => {
            store._toastError('DRAFTS.STATUS.MARK_FAILED');
            return of(undefined);
          }),
        );

      const save = rxMethod<SaveRequest>(
        pipe(
          tap(() => store._setSaving(true)),
          exhaustMap(({ value }) => {
            const payload = store.payload();
            const existing = store.existing();
            if (payload === undefined) {
              store._setSaving(false);
              return EMPTY;
            }
            const body = { code: value.code.trim(), severity: value.severity, description: value.description };
            const options = skipErrorToast();
            const write$: Observable<unknown> =
              existing === null
                ? statusesApi.create(payload.machineId, body, options)
                : statusesApi.update(payload.machineId, existing.id, body, options);
            return write$.pipe(
              concatMap(() => mark$(payload.draftId)),
              map(() => undefined),
              tap(() => {
                store._toastSuccess(existing === null ? 'DRAFTS.STATUS.CREATED' : 'DRAFTS.STATUS.UPDATED');
                actions.complete(DraftActionType.Changed);
              }),
              catchError((error: unknown) => {
                store._toastError(httpErrorKey(error, { [HttpStatusCode.Conflict]: 'DRAFTS.STATUS.CODE_TAKEN' }));
                return EMPTY;
              }),
              finalize(() => store._setSaving(false)),
            );
          }),
        ),
      );

      return {
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
          actions.cancel();
        },
      };
    }),
  );
}
