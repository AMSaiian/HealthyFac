import { HttpContext } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { EMPTY, exhaustMap, type Observable, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { withGridState } from '@shared/grid/with-grid-state';
import { AuthStore } from '@shared/security/auth.store';
import { Permission } from '@shared/security/permissions';
import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';
import { CLOCK } from '@shared/time/clock';

import type { Machine } from '@machines/machine';
import type {
  MachineErrorCode,
  MachineErrorCodeColumn,
  MachineErrorCodeFilter,
  MachineErrorCodeSortColumn,
} from '@machines/error-codes/machine-error-code';
import { toSearchBody } from '@machines/error-codes/list/machine-error-code-query';
import { type ErrorCodeEditor, INITIAL_GRID } from '@machines/error-codes/list/machine-error-codes-grid.store.utils';
import { MachineErrorCodesApi } from '@machines/error-codes/machine-error-codes-api';
import { MachinesApi } from '@machines/machines-api';

interface ScopeState {
  machineId: string | null;
  machine: Machine | null;
  editor: ErrorCodeEditor | null;
}

export const MachineErrorCodesGridStore = signalStore(
  withGridState<MachineErrorCodeFilter, MachineErrorCodeSortColumn, MachineErrorCodeColumn>(INITIAL_GRID),
  withState<ScopeState>({ machineId: null, machine: null, editor: null }),
  withProps(() => ({
    _auth: inject(AuthStore),
    _clock: inject(CLOCK),
    _api: inject(MachineErrorCodesApi),
    _machinesApi: inject(MachinesApi),
    _confirmation: inject(ConfirmationService),
    _messages: inject(MessageService),
    _translate: inject(TranslateService),
  })),
  withComputed((store) => ({
    canWrite: computed(() => store._auth.hasPermission(Permission.MachineErrorCodesWrite)),
    canDelete: computed(() => store._auth.hasPermission(Permission.MachineErrorCodesDelete)),
  })),
  withProps((store) => ({
    errorCodes: rxResource({
      params: () => {
        const machineId = store.machineId();
        return machineId === null
          ? undefined
          : {
              machineId,
              body: toSearchBody(
                { offset: store.offset(), limit: store.limit(), sort: store.sort(), filter: store.filter() },
                store._clock.timeZoneId(),
              ),
            };
      },
      stream: ({ params }) => store._api.search(params.machineId, params.body),
    }),
  })),
  withMethods((store) => {
    const deleteErrorCode = rxMethod<MachineErrorCode>(
      pipe(
        exhaustMap((errorCode) =>
          store._api.delete(store.machineId() ?? '', errorCode.id).pipe(
            tap(() => {
              store._messages.add({
                severity: 'success',
                summary: store._translate.instant('MACHINE_ERROR_CODES.TOASTS.SUCCESS'),
                detail: store._translate.instant('MACHINE_ERROR_CODES.TOASTS.DELETED'),
              });
              store.errorCodes.reload();
            }),
            catchError(() => EMPTY),
          ),
        ),
      ),
    );

    return {
      /**
       * Starts a visit to the error codes of a machine: loads the machine, then resets the grid and closes the dialog.
       * The state changes only after a successful load, so a pending or failed load sends no search and keeps the
       * current machine. The load skips the error toast, so the caller decides what a failure means.
       */
      openMachine(machineId: string): Observable<Machine> {
        return store._machinesApi.get(machineId, { context: new HttpContext().set(SKIP_ERROR_TOAST, true) }).pipe(
          tap((machine) => {
            patchState(store, { machineId, machine, editor: null });
            store.resetGrid();
          }),
        );
      },
      requestCreate(): void {
        patchState(store, { editor: { errorCodeId: null } });
      },
      requestUpdate(id: string): void {
        patchState(store, { editor: { errorCodeId: id } });
      },
      closeEditor(): void {
        patchState(store, { editor: null });
      },
      onSaved(): void {
        patchState(store, { editor: null });
        store.errorCodes.reload();
      },
      requestDelete(errorCode: MachineErrorCode): void {
        store._confirmation.confirm({
          header: store._translate.instant('MACHINE_ERROR_CODES.DELETE_HEADER'),
          message: store._translate.instant('MACHINE_ERROR_CODES.DELETE_MESSAGE', { code: errorCode.code }),
          icon: 'pi pi-exclamation-triangle',
          acceptLabel: store._translate.instant('MACHINE_ERROR_CODES.DELETE'),
          rejectLabel: store._translate.instant('ACTIONS.CANCEL'),
          acceptButtonProps: { severity: 'danger' },
          rejectButtonProps: { severity: 'secondary' },
          accept: () => {
            deleteErrorCode(errorCode);
          },
        });
      },
    };
  }),
);
