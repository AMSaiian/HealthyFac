import { HttpContext, HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { EMPTY, exhaustMap, finalize, type Observable, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';
import { httpErrorMessageKey } from '@shared/http/http-error-message';
import { draftConflictKey } from '@machines/drafts/draft-conflict';
import { DraftsApi } from '@machines/drafts/drafts-api';
import { MachineActionType } from '@machines/machine-actions';
import { MachinesApi } from '@machines/machines-api';

import { EMPTY_VALUE, fromDraftValue, overlay, payloadOf, toBody, toFormValue } from './machine-drawer.store.utils';
import type { MachineFormValue } from '@machines/drawer/machine-form/machine-form-schema';

const SUCCESS_SUMMARY_KEY = 'MACHINES.TOASTS.SUCCESS';
const ERROR_SUMMARY_KEY = 'ERRORS.SUMMARY';

interface DrawerState {
  action: AppAction | null;
  isSaving: boolean;
}

export const MachineDrawerStore = signalStore(
  withState<DrawerState>({ action: null, isSaving: false }),
  withProps(() => ({
    _api: inject(MachinesApi),
    _draftsApi: inject(DraftsApi),
    _actions: inject(ActionStore),
    _messages: inject(MessageService),
    _translate: inject(TranslateService),
  })),
  withComputed((store) => ({
    isUpdate: computed(() => store.action()?.type === MachineActionType.Update),
    machineId: computed(() => payloadOf(store.action()).machineId),
    draftId: computed(() => payloadOf(store.action()).draftId),
  })),
  withProps((store) => ({
    machine: rxResource({
      params: () => (store.isUpdate() ? store.machineId() : undefined),
      stream: ({ params }) => store._api.get(params),
    }),
    /** The create draft that the drawer applies. A drawer without a draft never loads it. */
    draft: rxResource({
      params: () => (store.isUpdate() ? undefined : store.draftId()),
      stream: ({ params }) => store._draftsApi.getMachineDraft(params),
    }),
  })),
  withComputed((store) => ({
    initialValue: computed((): MachineFormValue | null => {
      if (!store.isUpdate()) {
        if (store.draftId() !== undefined) {
          return store.draft.hasValue() ? fromDraftValue(store.draft.value().draft) : null;
        }
        return store.action() === null ? null : EMPTY_VALUE;
      }
      return store.machine.hasValue() ? toFormValue(store.machine.value()) : null;
    }),
  })),
  withComputed((store) => ({
    prefilledValue: computed((): MachineFormValue | null => {
      const initial = store.initialValue();
      return initial === null ? null : overlay(initial, payloadOf(store.action()));
    }),
  })),
  withComputed((store) => ({
    gatewayId: computed((): string | null => (store.machine.hasValue() ? store.machine.value().gatewayId : null)),
  })),
  withMethods((store) => {
    const showError = (detailKey: string): void => {
      store._messages.add({
        severity: 'error',
        summary: store._translate.instant(ERROR_SUMMARY_KEY),
        detail: store._translate.instant(detailKey),
      });
    };

    const request$ = (value: MachineFormValue): Observable<unknown> => {
      const body = toBody(value);
      const options = { context: new HttpContext().set(SKIP_ERROR_TOAST, true) };
      const draftId = store.draftId();
      if (draftId !== undefined && !store.isUpdate()) {
        return store._draftsApi.applyMachineDraft(draftId, body, options);
      }
      return store.isUpdate()
        ? store._api.update(store.machineId() ?? '', body, options)
        : store._api.create(body, options);
    };

    return {
      start(action: AppAction): void {
        patchState(store, { action });
      },
      save: rxMethod<MachineFormValue>(
        pipe(
          tap(() => patchState(store, { isSaving: true })),
          exhaustMap((value) =>
            request$(value).pipe(
              tap(() => {
                store._messages.add({
                  severity: 'success',
                  summary: store._translate.instant(SUCCESS_SUMMARY_KEY),
                  detail: store._translate.instant(
                    store.isUpdate() ? 'MACHINES.TOASTS.UPDATED' : 'MACHINES.TOASTS.CREATED',
                  ),
                });
                store._actions.complete(MachineActionType.Saved);
              }),
              catchError((error: unknown) => {
                const status = error instanceof HttpErrorResponse ? error.status : 0;
                showError(
                  draftConflictKey(error) ??
                    (status === HttpStatusCode.Conflict
                      ? 'MACHINES.TOASTS.SERIAL_NUMBER_TAKEN'
                      : httpErrorMessageKey(status)),
                );
                return EMPTY;
              }),
              finalize(() => patchState(store, { isSaving: false })),
            ),
          ),
        ),
      ),
      cancel(): void {
        store._actions.cancel();
      },
    };
  }),
);
