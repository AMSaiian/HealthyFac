import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { EMPTY, exhaustMap, finalize, type Observable, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { httpErrorMessageKey } from '@shared/http/http-error-message';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';

import type { InviteFormValue } from '@employees/invites/invite-dialog/invite-dialog-schema';
import { EmployeeActionType, OPEN_INVITE_EXISTS_DETAIL } from '@employees/employee-actions';
import { EmployeesApi } from '@employees/employees-api';

const SUCCESS_SUMMARY_KEY = 'WORK_ORDERS.TOASTS.SUCCESS';
const ERROR_SUMMARY_KEY = 'ERRORS.SUMMARY';

interface ActionState {
  action: AppAction | null;
  isSaving: boolean;
}

interface WriteRequest {
  readonly request$: Observable<unknown>;
  readonly successKey: string;
  readonly isDialogOpen: boolean;
  readonly conflictKey?: string;
}

const payloadOf = (action: AppAction | null): { employeeId?: string; inviteId?: string; rowVersion?: number } =>
  (action?.payload ?? {}) as { employeeId?: string; inviteId?: string; rowVersion?: number };

function detailOf(error: HttpErrorResponse): string | null {
  const body: unknown = error.error;
  const detail = typeof body === 'object' && body !== null ? (body as Record<string, unknown>)['detail'] : null;
  return typeof detail === 'string' ? detail : null;
}

/**
 * Runs the actions of the employees page: the invite dialog, the resend of an invite, and the retry of the
 * provisioning. A finished action completes with `employees.saved`, so the grids reload.
 */
export const EmployeeActionsStore = signalStore(
  withState<ActionState>({ action: null, isSaving: false }),
  withProps(() => ({
    _api: inject(EmployeesApi),
    _actions: inject(ActionStore),
    _messages: inject(MessageService),
    _translate: inject(TranslateService),
  })),
  withComputed((store) => ({
    isInviteDialog: computed(() => store.action()?.type === EmployeeActionType.Invite),
  })),
  withMethods((store) => {
    const localDateTime = new LocalDateTimePipe();
    const showError = (detailKey: string): void => {
      store._messages.add({
        severity: 'error',
        summary: store._translate.instant(ERROR_SUMMARY_KEY),
        detail: store._translate.instant(detailKey),
      });
    };

    const run = rxMethod<WriteRequest>(
      pipe(
        tap(() => patchState(store, { isSaving: true })),
        exhaustMap(({ request$, successKey, isDialogOpen, conflictKey }) =>
          request$.pipe(
            tap((result) => {
              const expiresAt = (result as { expiresAt?: string } | null)?.expiresAt;
              store._messages.add({
                severity: 'success',
                summary: store._translate.instant(SUCCESS_SUMMARY_KEY),
                detail: store._translate.instant(successKey, {
                  expiresAt: expiresAt === undefined ? '' : localDateTime.transform(expiresAt),
                }),
              });
              store._actions.complete(EmployeeActionType.Saved);
            }),
            catchError((error: unknown) => {
              const status = error instanceof HttpErrorResponse ? error.status : 0;
              if (status === HttpStatusCode.Conflict) {
                const isOpenInvite =
                  error instanceof HttpErrorResponse && detailOf(error) === OPEN_INVITE_EXISTS_DETAIL;
                showError(
                  isOpenInvite
                    ? 'EMPLOYEES.TOASTS.OPEN_INVITE_EXISTS'
                    : (conflictKey ?? 'EMPLOYEES.TOASTS.ALREADY_EMPLOYEE'),
                );
              } else {
                showError(
                  status === HttpStatusCode.UnprocessableEntity ? 'EMPLOYEES.TOASTS.RULE' : httpErrorMessageKey(status),
                );
              }
              if (!isDialogOpen) {
                store._actions.cancel();
              }
              return EMPTY;
            }),
            finalize(() => patchState(store, { isSaving: false })),
          ),
        ),
      ),
    );

    return {
      start(action: AppAction): void {
        patchState(store, { action });
        const { employeeId, inviteId, rowVersion } = payloadOf(action);
        if (action.type === EmployeeActionType.Resend && inviteId !== undefined) {
          run({
            request$: store._api.resendInvite(inviteId),
            successKey: 'EMPLOYEES.TOASTS.RESENT',
            isDialogOpen: false,
          });
        } else if (action.type === EmployeeActionType.Revoke && inviteId !== undefined && rowVersion !== undefined) {
          run({
            request$: store._api.revokeInvite(inviteId, rowVersion),
            successKey: 'EMPLOYEES.TOASTS.REVOKED',
            isDialogOpen: false,
            conflictKey: 'EMPLOYEES.TOASTS.INVITE_CHANGED',
          });
        } else if (action.type === EmployeeActionType.RetryProvisioning && employeeId !== undefined) {
          run({
            request$: store._api.retryProvisioning(employeeId),
            successKey: 'EMPLOYEES.TOASTS.RETRIED',
            isDialogOpen: false,
          });
        }
      },
      submitInvite(value: InviteFormValue): void {
        run({
          request$: store._api.createInvite({
            email: value.email.trim(),
            roleCode: value.roleCode,
            ...(value.firstName.trim() === '' ? {} : { firstName: value.firstName.trim() }),
            ...(value.lastName.trim() === '' ? {} : { lastName: value.lastName.trim() }),
          }),
          successKey: 'EMPLOYEES.TOASTS.INVITED',
          isDialogOpen: true,
        });
      },
      cancel(): void {
        store._actions.cancel();
      },
    };
  }),
);
