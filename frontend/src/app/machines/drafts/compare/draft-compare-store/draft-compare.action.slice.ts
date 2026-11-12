import { HttpStatusCode } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStoreFeature, type, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, finalize, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { safeParse } from 'zod/mini';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { hasFormChanges } from '@shared/forms/form-changes';
import { httpErrorKey, isHttpStatus, skipErrorToast } from '@shared/http/http-error-key';

import type { DraftCompareActionPayload } from '@machines/drafts/draft-action-payloads';
import type { MachineDraftResponse } from '@machines/drafts/draft';
import { DraftsApi } from '@machines/drafts/drafts-api';
import {
  type CompareChoices,
  type CompareField,
  mergeValues,
  NO_CHOICES,
  toCompareRows,
  toCurrentFormValue,
} from '@machines/drafts/compare/draft-compare-store/draft-compare.store.utils';
import { machineFormSchema } from '@machines/drawer/machine-form/machine-form-schema';
import { MachineActionType } from '@machines/machine-actions';

interface ActionState {
  action: AppAction | null;
  choices: CompareChoices;
  isSaving: boolean;
}

/**
 * The open compare: the action, the choices of the user, the loaded draft, the merged value that **Save** writes, the
 * save in progress, and the save that applies the merged value to the machine.
 */
export function withDraftCompareAction() {
  return signalStoreFeature(
    {
      methods: type<{ _toastSuccess: (key: string) => void; _toastError: (key: string) => void }>(),
    },
    withState<ActionState>({ action: null, choices: NO_CHOICES, isSaving: false }),
    withComputed((store) => ({
      draftId: computed(() => (store.action()?.payload as DraftCompareActionPayload | undefined)?.draftId),
    })),
    withProps((store) => {
      const draftsApi = inject(DraftsApi);
      return {
        data: rxResource({
          params: () => store.draftId(),
          stream: ({ params }) => draftsApi.getMachineDraft(params),
        }),
      };
    }),
    withComputed((store) => ({
      current: computed((): MachineDraftResponse['current'] => {
        return store.data.hasValue() ? store.data.value().current : null;
      }),
    })),
    withComputed((store) => {
      const rows = computed(() => {
        const response = store.data.hasValue() ? store.data.value() : null;
        return response?.current == null ? [] : toCompareRows(response.draft, response.current, store.choices());
      });
      const merged = computed(() => {
        const current = store.current();
        return current == null ? null : mergeValues(current, rows());
      });
      return { rows, merged };
    }),
    withComputed((store) => ({
      /** The reason that **Save** is off: nothing is taken, or a merged value breaks the machine rules. */
      saveDisabledReason: computed((): string | null => {
        const current = store.current();
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
    withMethods((store) => ({
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
      _resetChoices(): void {
        patchState(store, { choices: NO_CHOICES });
      },
      _reloadData(): void {
        store.data.reload();
      },
      _setSaving(isSaving: boolean): void {
        patchState(store, { isSaving });
      },
    })),
    withMethods((store) => {
      const writeApi = inject(DraftsApi);
      const actions = inject(ActionStore);

      const saveMerged = rxMethod<void>(
        pipe(
          tap(() => store._setSaving(true)),
          exhaustMap(() => {
            const draftId = store.draftId();
            const merged = store.merged();
            const current = store.current();
            if (draftId === undefined || merged === null || current == null) {
              store._setSaving(false);
              return EMPTY;
            }
            return writeApi
              .applyMachineDraft(
                draftId,
                {
                  name: merged.name.trim(),
                  serialNumber: merged.serialNumber.trim(),
                  description: merged.description,
                  rowVersion: current.rowVersion,
                },
                skipErrorToast(),
              )
              .pipe(
                tap(() => {
                  store._toastSuccess('DRAFTS.COMPARE.APPLIED');
                  actions.complete(MachineActionType.Saved);
                }),
                catchError((error: unknown) => {
                  store._toastError(httpErrorKey(error, { [HttpStatusCode.Conflict]: 'DRAFTS.COMPARE.CONFLICT' }));
                  if (isHttpStatus(error, HttpStatusCode.Conflict)) {
                    store._resetChoices();
                    store._reloadData();
                  }
                  return EMPTY;
                }),
                finalize(() => store._setSaving(false)),
              );
          }),
        ),
      );

      return {
        save(): void {
          if (store.saveDisabledReason() === null) {
            saveMerged();
          }
        },
        cancel(): void {
          actions.cancel();
        },
      };
    }),
  );
}
