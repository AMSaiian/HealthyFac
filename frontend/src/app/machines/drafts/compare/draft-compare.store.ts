import { HttpContext, HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { EMPTY, exhaustMap, finalize, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { safeParse } from 'zod/mini';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { hasFormChanges } from '@shared/forms/form-changes';
import { httpErrorMessageKey } from '@shared/http/http-error-message';
import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';

import type { DraftCompareActionPayload } from '@machines/drafts/draft-action-payloads';
import { DraftsApi } from '@machines/drafts/drafts-api';
import {
  type CompareChoices,
  type CompareField,
  mergeValues,
  NO_CHOICES,
  toCompareRows,
  toCurrentFormValue,
} from '@machines/drafts/compare/draft-compare.store.utils';
import { machineFormSchema } from '@machines/drawer/machine-form/machine-form-schema';
import { MachineActionType } from '@machines/machine-actions';

interface CompareState {
  action: AppAction | null;
  choices: CompareChoices;
  isSaving: boolean;
}

export const DraftCompareStore = signalStore(
  withState<CompareState>({ action: null, choices: NO_CHOICES, isSaving: false }),
  withProps(() => ({
    _api: inject(DraftsApi),
    _actions: inject(ActionStore),
    _messages: inject(MessageService),
    _translate: inject(TranslateService),
  })),
  withComputed((store) => ({
    draftId: computed(() => (store.action()?.payload as DraftCompareActionPayload | undefined)?.draftId),
  })),
  withProps((store) => ({
    data: rxResource({
      params: () => store.draftId(),
      stream: ({ params }) => store._api.getMachineDraft(params),
    }),
  })),
  withComputed((store) => {
    const rows = computed(() => {
      const response = store.data.hasValue() ? store.data.value() : null;
      return response?.current == null ? [] : toCompareRows(response.draft, response.current, store.choices());
    });
    const merged = computed(() => {
      const current = store.data.hasValue() ? store.data.value().current : null;
      return current == null ? null : mergeValues(current, rows());
    });
    return { rows, merged };
  }),
  withComputed((store) => ({
    /** The reason that **Save** is off: nothing is taken, or a merged value breaks the machine rules. */
    saveDisabledReason: computed((): string | null => {
      const current = store.data.hasValue() ? store.data.value().current : null;
      const merged = store.merged();
      if (current == null || merged === null) {
        return 'DRAFTS.COMPARE.NO_CHANGES';
      }
      if (!safeParse(machineFormSchema, merged).success) {
        return 'DRAFTS.COMPARE.INVALID';
      }
      return hasFormChanges(machineFormSchema, toCurrentFormValue(current), merged)
        ? null
        : 'DRAFTS.COMPARE.NO_CHANGES';
    }),
  })),
  withMethods((store) => {
    const showError = (detailKey: string): void => {
      store._messages.add({
        severity: 'error',
        summary: store._translate.instant('ERRORS.SUMMARY'),
        detail: store._translate.instant(detailKey),
      });
    };

    const saveMerged = rxMethod<void>(
      pipe(
        tap(() => patchState(store, { isSaving: true })),
        exhaustMap(() => {
          const draftId = store.draftId();
          const merged = store.merged();
          const current = store.data.hasValue() ? store.data.value().current : null;
          if (draftId === undefined || merged === null || current == null) {
            patchState(store, { isSaving: false });
            return EMPTY;
          }
          return store._api
            .applyMachineDraft(
              draftId,
              {
                name: merged.name.trim(),
                serialNumber: merged.serialNumber.trim(),
                description: merged.description,
                rowVersion: current.rowVersion,
              },
              { context: new HttpContext().set(SKIP_ERROR_TOAST, true) },
            )
            .pipe(
              tap(() => {
                store._messages.add({
                  severity: 'success',
                  summary: store._translate.instant('FILES.TOASTS.SUCCESS'),
                  detail: store._translate.instant('DRAFTS.COMPARE.APPLIED'),
                });
                store._actions.complete(MachineActionType.Saved);
              }),
              catchError((error: unknown) => {
                const status = error instanceof HttpErrorResponse ? error.status : 0;
                if (status === HttpStatusCode.Conflict) {
                  showError('DRAFTS.COMPARE.CONFLICT');
                  patchState(store, { choices: NO_CHOICES });
                  store.data.reload();
                } else {
                  showError(httpErrorMessageKey(status));
                }
                return EMPTY;
              }),
              finalize(() => patchState(store, { isSaving: false })),
            );
        }),
      ),
    );

    return {
      start(action: AppAction): void {
        patchState(store, { action, choices: NO_CHOICES });
      },
      take(field: CompareField, isTaken: boolean): void {
        patchState(store, (state) => ({
          choices: { ...state.choices, taken: { ...state.choices.taken, [field]: isTaken } },
        }));
      },
      edit(field: CompareField, value: string | null): void {
        patchState(store, (state) => ({
          choices: { ...state.choices, edited: { ...state.choices.edited, [field]: value } },
        }));
      },
      save(): void {
        if (store.saveDisabledReason() === null) {
          saveMerged();
        }
      },
      cancel(): void {
        store._actions.cancel();
      },
    };
  }),
);
