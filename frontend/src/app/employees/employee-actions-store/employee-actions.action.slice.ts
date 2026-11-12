import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { patchState, signalStoreFeature, type, withComputed, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, finalize, type Observable, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { httpErrorKey } from '@shared/http/http-error-key';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';

import type { InviteFormValue } from '@employees/invites/invite-dialog/invite-dialog-schema';
import { EmployeeActionType, OPEN_INVITE_EXISTS_DETAIL } from '@employees/employee-actions';
import { EmployeesApi } from '@employees/employees-api';

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
 * The open action of the employees page and whether it is the invite dialog. The slice also runs the write of an
 * action (the invite, the resend, the revoke, the retry) and the cancel of its dialog, and owns the save in progress.
 */
export function withEmployeeActionsAction() {
  return signalStoreFeature(
    {
      methods: type<{
        _toastSuccess: (detailKey: string, params?: object) => void;
        _toastError: (detailKey: string, params?: object) => void;
      }>(),
    },
    withState<ActionState>({ action: null, isSaving: false }),
    withComputed((store) => ({
      isInviteDialog: computed(() => store.action()?.type === EmployeeActionType.Invite),
    })),
    withMethods((store) => ({
      _setAction(action: AppAction): void {
        patchState(store, { action });
      },
      _setSaving(isSaving: boolean): void {
        patchState(store, { isSaving });
      },
    })),
    withMethods((store) => {
      const api = inject(EmployeesApi);
      const actions = inject(ActionStore);
      const localDateTime = new LocalDateTimePipe();
      const run = rxMethod<WriteRequest>(
        pipe(
          tap(() => store._setSaving(true)),
          exhaustMap(({ request$, successKey, isDialogOpen, conflictKey }) =>
            request$.pipe(
              tap((result) => {
                const expiresAt = (result as { expiresAt?: string } | null)?.expiresAt;
                store._toastSuccess(successKey, {
                  expiresAt: expiresAt === undefined ? '' : localDateTime.transform(expiresAt),
                });
                actions.complete(EmployeeActionType.Saved);
              }),
              catchError((error: unknown) => {
                const isOpenInvite =
                  error instanceof HttpErrorResponse && detailOf(error) === OPEN_INVITE_EXISTS_DETAIL;
                store._toastError(
                  httpErrorKey(error, {
                    [HttpStatusCode.Conflict]: isOpenInvite
                      ? 'EMPLOYEES.TOASTS.OPEN_INVITE_EXISTS'
                      : (conflictKey ?? 'EMPLOYEES.TOASTS.ALREADY_EMPLOYEE'),
                    [HttpStatusCode.UnprocessableEntity]: 'EMPLOYEES.TOASTS.RULE',
                  }),
                );
                if (!isDialogOpen) {
                  actions.cancel();
                }
                return EMPTY;
              }),
              finalize(() => store._setSaving(false)),
            ),
          ),
        ),
      );

      return {
        _runAction(action: AppAction): void {
          const { employeeId, inviteId, rowVersion } = payloadOf(action);
          if (action.type === EmployeeActionType.Resend && inviteId !== undefined) {
            run({
              request$: api.resendInvite(inviteId),
              successKey: 'EMPLOYEES.TOASTS.RESENT',
              isDialogOpen: false,
            });
          } else if (action.type === EmployeeActionType.Revoke && inviteId !== undefined && rowVersion !== undefined) {
            run({
              request$: api.revokeInvite(inviteId, rowVersion),
              successKey: 'EMPLOYEES.TOASTS.REVOKED',
              isDialogOpen: false,
              conflictKey: 'EMPLOYEES.TOASTS.INVITE_CHANGED',
            });
          } else if (action.type === EmployeeActionType.RetryProvisioning && employeeId !== undefined) {
            run({
              request$: api.retryProvisioning(employeeId),
              successKey: 'EMPLOYEES.TOASTS.RETRIED',
              isDialogOpen: false,
            });
          }
        },
        submitInvite(value: InviteFormValue): void {
          run({
            request$: api.createInvite({
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
          actions.cancel();
        },
      };
    }),
  );
}
