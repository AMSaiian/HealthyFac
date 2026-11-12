import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { patchState, signalStoreFeature, type, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, finalize, type Observable, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import type { ConfirmRequest } from '@shared/confirm/with-confirm';
import { httpErrorKey } from '@shared/http/http-error-key';

import { EmployeesApi } from '@employees/employees-api';
import type { AffectedWorkOrder, ShiftOverride, ShiftVersion } from '@employees/shifts/shift';
import type { OverrideFormValue } from '@employees/shifts/override-dialog/override-dialog-schema';
import type { VersionFormValue } from '@employees/shifts/version-dialog/version-dialog-schema';
import {
  affectedOf,
  type ShiftDialog,
  toCreateBody,
  toOverrideBlocks,
} from '@employees/shifts/shifts-store/shifts.store.utils';

interface DialogState {
  dialog: ShiftDialog | null;
  affectedWorkOrders: readonly AffectedWorkOrder[];
  isSaving: boolean;
}

interface WriteRequest {
  readonly request$: Observable<unknown>;
  readonly successKey: string;
}

/** The delete confirmation keys of each kind of shift. Each key is a literal, so the translation check finds it. */
const DELETE_CONFIRM_KEYS = {
  VERSION: {
    headerKey: 'SHIFTS.CONFIRM.DELETE_VERSION_HEADER',
    messageKey: 'SHIFTS.CONFIRM.DELETE_VERSION_MESSAGE',
  },
  OVERRIDE: {
    headerKey: 'SHIFTS.CONFIRM.DELETE_OVERRIDE_HEADER',
    messageKey: 'SHIFTS.CONFIRM.DELETE_OVERRIDE_MESSAGE',
  },
} as const;

/**
 * The open version or override dialog, the work orders that a failed write names, and the save in progress. The save
 * runs in this slice, because a saved write closes the dialog.
 */
export function withShiftsDialog() {
  return signalStoreFeature(
    {
      state: type<{ employeeId: string | null }>(),
      props: type<{
        versions: { reload(): boolean };
        overrides: { reload(): boolean };
      }>(),
      methods: type<{
        _toastSuccess: (key: string) => void;
        _toastError: (key: string) => void;
        _confirmDestructive: (request: ConfirmRequest) => void;
      }>(),
    },
    withState<DialogState>({ dialog: null, affectedWorkOrders: [], isSaving: false }),
    withMethods((store) => ({
      _setDialog(dialog: ShiftDialog | null): void {
        patchState(store, { dialog });
      },
      _setAffectedWorkOrders(affectedWorkOrders: readonly AffectedWorkOrder[]): void {
        patchState(store, { affectedWorkOrders });
      },
      _setSaving(isSaving: boolean): void {
        patchState(store, { isSaving });
      },
      openVersionDialog(version: ShiftVersion | null): void {
        patchState(store, { dialog: { kind: 'version', version }, affectedWorkOrders: [] });
      },
      openOverrideDialog(override: ShiftOverride | null): void {
        patchState(store, { dialog: { kind: 'override', override }, affectedWorkOrders: [] });
      },
      closeDialog(): void {
        patchState(store, { dialog: null, affectedWorkOrders: [] });
      },
    })),
    withMethods((store) => {
      const api = inject(EmployeesApi);

      const reloadAll = (): void => {
        store.versions.reload();
        store.overrides.reload();
      };

      const write = rxMethod<WriteRequest>(
        pipe(
          tap(() => store._setSaving(true)),
          exhaustMap(({ request$, successKey }) =>
            request$.pipe(
              tap(() => {
                store._toastSuccess(successKey);
                store.closeDialog();
                reloadAll();
              }),
              catchError((error: unknown) => {
                const affected = affectedOf(error);
                if (affected !== null) {
                  store._setAffectedWorkOrders(affected);
                } else {
                  store._toastError(
                    httpErrorKey(error, {
                      [HttpStatusCode.Conflict]: 'SHIFTS.TOASTS.CONFLICT',
                      [HttpStatusCode.UnprocessableEntity]: 'SHIFTS.TOASTS.RULE',
                    }),
                  );
                  if (error instanceof HttpErrorResponse && error.status === HttpStatusCode.Conflict) {
                    store._setDialog(null);
                    reloadAll();
                  }
                }
                return EMPTY;
              }),
              finalize(() => store._setSaving(false)),
            ),
          ),
        ),
      );

      const confirmDelete = (kind: keyof typeof DELETE_CONFIRM_KEYS, accept: () => void): void => {
        store._confirmDestructive({
          ...DELETE_CONFIRM_KEYS[kind],
          acceptKey: 'ACTIONS.DELETE',
          onAccept: accept,
        });
      };

      return {
        saveVersion(value: VersionFormValue): void {
          const employeeId = store.employeeId();
          const dialog = store.dialog();
          if (employeeId === null || dialog?.kind !== 'version') {
            return;
          }
          const body = toCreateBody(value);
          write({
            request$:
              dialog.version === null
                ? api.createVersion(employeeId, body)
                : api.updateVersion(employeeId, dialog.version.id, {
                    days: body.days,
                    rowVersion: dialog.version.rowVersion,
                  }),
            successKey: 'SHIFTS.TOASTS.SAVED',
          });
        },
        saveOverride(value: OverrideFormValue): void {
          const employeeId = store.employeeId();
          const dialog = store.dialog();
          if (employeeId === null || dialog?.kind !== 'override' || value.date === null) {
            return;
          }
          write({
            request$: api.setOverride(employeeId, value.date.toString(), {
              blocks: toOverrideBlocks(value),
              ...(dialog.override === null ? {} : { rowVersion: dialog.override.rowVersion }),
            }),
            successKey: 'SHIFTS.TOASTS.SAVED',
          });
        },
        requestDeleteVersion(version: ShiftVersion): void {
          const employeeId = store.employeeId();
          if (employeeId !== null) {
            confirmDelete('VERSION', () =>
              write({
                request$: api.deleteVersion(employeeId, version.id, version.rowVersion),
                successKey: 'SHIFTS.TOASTS.DELETED',
              }),
            );
          }
        },
        requestDeleteOverride(override: ShiftOverride): void {
          const employeeId = store.employeeId();
          if (employeeId !== null) {
            confirmDelete('OVERRIDE', () =>
              write({
                request$: api.deleteOverride(employeeId, override.date, override.rowVersion),
                successKey: 'SHIFTS.TOASTS.DELETED',
              }),
            );
          }
        },
      };
    }),
  );
}
