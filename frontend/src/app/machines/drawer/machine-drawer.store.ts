import { HttpStatusCode } from '@angular/common/http';
import { computed, effect, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, finalize, type Observable, pipe, tap, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { withDetailsMode } from '@shared/forms/with-details-mode';
import { httpErrorKey, isHttpStatus, skipErrorToast } from '@shared/http/http-error-key';
import { descriptionLinks } from '@shared/rich-text/file-image/description-links';
import { RichTextImageLinks } from '@shared/rich-text/file-image/rich-text-image-links';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';
import { CLOCK } from '@shared/time/clock';
import { withToasts } from '@shared/toasts/with-toasts';
import { draftConflictKey } from '@machines/drafts/draft-conflict';
import { DraftsApi } from '@machines/drafts/drafts-api';
import { MachineActionType } from '@machines/machine-actions';
import { MachinesApi } from '@machines/machines-api';

import {
  emptyValue,
  fromDraftValue,
  minGoLiveOn,
  overlay,
  payloadOf,
  toBody,
  toFormValue,
  toUpdateBody,
} from './machine-drawer.store.utils';
import type { MachineFormValue } from '@machines/drawer/machine-form/machine-form-schema';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

interface DrawerState {
  action: AppAction | null;
  isSaving: boolean;
}

export const MachineDrawerStore = signalStore(
  withState<DrawerState>({ action: null, isSaving: false }),
  withDetailsMode(),
  withProps(() => ({
    _api: inject(MachinesApi),
    _draftsApi: inject(DraftsApi),
    _actions: inject(ActionStore),
    _auth: inject(AuthStore),
    _clock: inject(CLOCK),
  })),
  withToasts(),
  withComputed((store) => ({
    isUpdate: computed(() => store.action()?.type === MachineActionType.Update),
    machineId: computed(() => payloadOf(store.action()).machineId),
    draftId: computed(() => payloadOf(store.action()).draftId),
    today: computed(() => store._clock.now().toZonedDateTimeISO(store._clock.timeZoneId()).toPlainDate()),
    canEdit: computed(() => store._auth.hasPermission(Permission.MachinesWrite)),
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
          return store.draft.hasValue() ? fromDraftValue(store.draft.value().draft, store.today()) : null;
        }
        return store.action() === null ? null : emptyValue(store.today());
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
  withProps((store) => ({
    descriptionLinks: descriptionLinks(
      () => (store.machine.hasValue() ? store.machine.value().description : null),
      inject(RichTextImageLinks),
    ).links,
  })),
  withComputed((store) => ({
    /** Tells whether the pencil shows: the loaded item in view mode, for a user who may change it. */
    isEditShown: computed(() => store.isViewMode() && store.isUpdate() && store.machine.hasValue() && store.canEdit()),
    gatewayId: computed((): string | null => (store.machine.hasValue() ? store.machine.value().gatewayId : null)),
    /** The first day that the go-live picker allows: today on create, the creation day on edit. */
    minGoLiveOn: computed(() =>
      minGoLiveOn(
        store.isUpdate() && store.machine.hasValue() ? store.machine.value() : null,
        store.today(),
        store._clock.timeZoneId(),
      ),
    ),
  })),
  withMethods((store) => {
    const request$ = (value: MachineFormValue): Observable<unknown> => {
      const body = toBody(value);
      const options = skipErrorToast();
      const draftId = store.draftId();
      if (draftId !== undefined && !store.isUpdate()) {
        return store._draftsApi.applyMachineDraft(draftId, body, options);
      }
      if (store.isUpdate()) {
        return value.state === null
          ? throwError(() => new Error('The update form has no state'))
          : store._api.update(store.machineId() ?? '', toUpdateBody(value, value.state), options);
      }
      return store._api.create(body, options);
    };

    return {
      start(action: AppAction): void {
        patchState(store, { action });
        store._startMode(action.type === MachineActionType.Update);
      },
      save: rxMethod<MachineFormValue>(
        pipe(
          tap(() => patchState(store, { isSaving: true })),
          exhaustMap((value) =>
            request$(value).pipe(
              tap(() => {
                if (store.isUpdate()) {
                  store._toastSuccess('MACHINES.TOASTS.UPDATED');
                  store._actions.notify(MachineActionType.Saved);
                  store.machine.reload();
                  store.view();
                } else {
                  store._toastSuccess('MACHINES.TOASTS.CREATED');
                  store._actions.complete(MachineActionType.Saved);
                }
              }),
              catchError((error: unknown) => {
                store._toastError(
                  draftConflictKey(error) ??
                    httpErrorKey(error, { [HttpStatusCode.Conflict]: 'MACHINES.TOASTS.SERIAL_NUMBER_TAKEN' }),
                );
                return EMPTY;
              }),
              finalize(() => patchState(store, { isSaving: false })),
            ),
          ),
        ),
      ),
      cancelEdit(): void {
        store.view();
      },
      cancel(): void {
        store._actions.cancel();
      },
    };
  }),
  withHooks({
    onInit(store) {
      effect(() => {
        if (isHttpStatus(store.machine.error(), HttpStatusCode.NotFound)) {
          store._actions.cancel();
        }
      });
    },
  }),
  withStoreDevtools('machine-drawer'),
);
