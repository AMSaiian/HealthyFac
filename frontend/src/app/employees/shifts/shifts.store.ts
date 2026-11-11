import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { EMPTY, exhaustMap, finalize, type Observable, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import { httpErrorMessageKey } from '@shared/http/http-error-message';
import { AuthStore } from '@shared/security/auth.store';
import { Permission } from '@shared/security/permissions';
import { CLOCK } from '@shared/time/clock';
import type { Temporal } from '@shared/time/temporal';

import { WorkOrderActionType } from '@work-orders/work-order-actions';
import { EmployeesApi } from '@employees/employees-api';
import type { AffectedWorkOrder, ShiftOverride, ShiftVersion } from '@employees/shifts/shift';
import type { OverrideFormValue } from '@employees/shifts/override-dialog/override-dialog-schema';
import type { VersionFormValue } from '@employees/shifts/version-dialog/version-dialog-schema';
import {
  affectedOf,
  isEditableVersion,
  monthRange,
  toCreateBody,
  toOverrideBlocks,
} from '@employees/shifts/shifts.store.utils';

const SUCCESS_SUMMARY_KEY = 'WORK_ORDERS.TOASTS.SUCCESS';
const ERROR_SUMMARY_KEY = 'ERRORS.SUMMARY';

export type ShiftDialog =
  | { readonly kind: 'version'; readonly version: ShiftVersion | null }
  | { readonly kind: 'override'; readonly override: ShiftOverride | null };

interface ShiftsState {
  employeeId: string | null;
  month: Temporal.PlainDate | null;
  dialog: ShiftDialog | null;
  affectedWorkOrders: readonly AffectedWorkOrder[];
  isSaving: boolean;
}

interface WriteRequest {
  readonly request$: Observable<unknown>;
  readonly successKey: string;
}

/** The versions and the overrides of the shifts of one employee. A supervisor changes them; a worker reads them. */
export const ShiftsStore = signalStore(
  withState<ShiftsState>({ employeeId: null, month: null, dialog: null, affectedWorkOrders: [], isSaving: false }),
  withProps(() => ({
    _api: inject(EmployeesApi),
    _auth: inject(AuthStore),
    _clock: inject(CLOCK),
    _actions: inject(ActionStore),
    _confirmation: inject(ConfirmationService),
    _messages: inject(MessageService),
    _translate: inject(TranslateService),
  })),
  withComputed((store) => ({
    canManage: computed(() => store._auth.hasPermission(Permission.ShiftsManage)),
    canGoBack: computed(() => store._auth.hasPermission(Permission.EmployeesRead)),
    today: computed(() => store._clock.now().toZonedDateTimeISO(store._clock.timeZoneId()).toPlainDate()),
  })),
  withProps((store) => ({
    versions: rxResource({
      params: () => store.employeeId() ?? undefined,
      stream: ({ params }) => store._api.shiftVersions(params),
    }),
    overrides: rxResource({
      params: () => {
        const employeeId = store.employeeId();
        const month = store.month();
        return employeeId === null || month === null ? undefined : { employeeId, month };
      },
      stream: ({ params }) => {
        const range = monthRange(params.month);
        return store._api.shiftOverrides(params.employeeId, range.from, range.to);
      },
    }),
  })),
  withMethods((store) => {
    const showError = (detailKey: string): void => {
      store._messages.add({
        severity: 'error',
        summary: store._translate.instant(ERROR_SUMMARY_KEY),
        detail: store._translate.instant(detailKey),
      });
    };

    const reloadAll = (): void => {
      store.versions.reload();
      store.overrides.reload();
    };

    const write = rxMethod<WriteRequest>(
      pipe(
        tap(() => patchState(store, { isSaving: true })),
        exhaustMap(({ request$, successKey }) =>
          request$.pipe(
            tap(() => {
              store._messages.add({
                severity: 'success',
                summary: store._translate.instant(SUCCESS_SUMMARY_KEY),
                detail: store._translate.instant(successKey),
              });
              patchState(store, { dialog: null, affectedWorkOrders: [] });
              reloadAll();
            }),
            catchError((error: unknown) => {
              const affected = affectedOf(error);
              const status = error instanceof HttpErrorResponse ? error.status : 0;
              if (affected !== null) {
                patchState(store, { affectedWorkOrders: affected });
              } else if (status === HttpStatusCode.Conflict) {
                showError('SHIFTS.TOASTS.CONFLICT');
                patchState(store, { dialog: null });
                reloadAll();
              } else {
                showError(
                  status === HttpStatusCode.UnprocessableEntity ? 'SHIFTS.TOASTS.RULE' : httpErrorMessageKey(status),
                );
              }
              return EMPTY;
            }),
            finalize(() => patchState(store, { isSaving: false })),
          ),
        ),
      ),
    );

    const confirmDelete = (kind: 'VERSION' | 'OVERRIDE', accept: () => void): void => {
      store._confirmation.confirm({
        header: store._translate.instant(`SHIFTS.CONFIRM.DELETE_${kind}_HEADER`),
        message: store._translate.instant(`SHIFTS.CONFIRM.DELETE_${kind}_MESSAGE`),
        icon: 'pi pi-exclamation-triangle',
        acceptLabel: store._translate.instant('SHIFTS.CONFIRM.DELETE'),
        rejectLabel: store._translate.instant('ACTIONS.CANCEL'),
        acceptButtonProps: { severity: 'danger' },
        rejectButtonProps: { severity: 'secondary' },
        accept,
      });
    };

    return {
      setEmployee(employeeId: string): void {
        patchState(store, { employeeId, month: store.month() ?? store.today() });
      },
      previousMonth(): void {
        const month = store.month();
        if (month !== null) {
          patchState(store, { month: month.subtract({ months: 1 }) });
        }
      },
      nextMonth(): void {
        const month = store.month();
        if (month !== null) {
          patchState(store, { month: month.add({ months: 1 }) });
        }
      },
      isEditable(version: ShiftVersion): boolean {
        return store.canManage() && isEditableVersion(version, store.today());
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
              ? store._api.createVersion(employeeId, body)
              : store._api.updateVersion(employeeId, dialog.version.id, {
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
          request$: store._api.setOverride(employeeId, value.date.toString(), {
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
              request$: store._api.deleteVersion(employeeId, version.id, version.rowVersion),
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
              request$: store._api.deleteOverride(employeeId, override.date, override.rowVersion),
              successKey: 'SHIFTS.TOASTS.DELETED',
            }),
          );
        }
      },
      openWorkOrder(id: string): void {
        void store._actions.dispatch({ type: WorkOrderActionType.Open, payload: { workOrderId: id } });
      },
    };
  }),
);
