import { effect, inject, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService } from 'primeng/api';
import { patchState, signalStore, withHooks, withMethods, withProps, withState } from '@ngrx/signals';

import { ActionStore } from '@shared/actions/action.store';
import { withGridState } from '@shared/grid/with-grid-state';

import type { InviteColumn, InviteFilter, InviteRow, InviteSortColumn } from '@employees/employee';
import { EmployeeActionType } from '@employees/employee-actions';
import { EmployeesApi } from '@employees/employees-api';
import { INITIAL_GRID, toSearchBody } from '@employees/invites/invites-grid.store.utils';

/** The grid state and the rows of the pending invites. */
export const InvitesGridStore = signalStore(
  withGridState<InviteFilter, InviteSortColumn, InviteColumn>(INITIAL_GRID),
  withState({ isActive: false }),
  withProps(() => ({
    _actions: inject(ActionStore),
    _api: inject(EmployeesApi),
    _confirmation: inject(ConfirmationService),
    _translate: inject(TranslateService),
  })),
  withProps((store) => ({
    invites: rxResource({
      params: () =>
        store.isActive()
          ? toSearchBody({ offset: store.offset(), limit: store.limit(), sort: store.sort(), filter: store.filter() })
          : undefined,
      stream: ({ params }) => store._api.searchInvites(params),
    }),
  })),
  withMethods((store) => ({
    /** Starts the loading: the tab of the pending invites calls it when it opens, so a closed tab sends no request. */
    activate(): void {
      patchState(store, { isActive: true });
    },
    /** Asks to confirm, then starts the revoke action with the row version that the row showed. */
    requestRevoke(row: InviteRow): void {
      store._confirmation.confirm({
        header: store._translate.instant('EMPLOYEES.REVOKE_HEADER'),
        message: store._translate.instant('EMPLOYEES.REVOKE_MESSAGE', { email: row.email }),
        icon: 'pi pi-exclamation-triangle',
        acceptLabel: store._translate.instant('EMPLOYEES.REVOKE'),
        rejectLabel: store._translate.instant('ACTIONS.CANCEL'),
        acceptButtonProps: { severity: 'danger' },
        rejectButtonProps: { severity: 'secondary' },
        accept: () => {
          void store._actions.dispatch({
            type: EmployeeActionType.Revoke,
            payload: { inviteId: row.id, rowVersion: row.rowVersion },
          });
        },
      });
    },
    requestResend(inviteId: string): void {
      void store._actions.dispatch({ type: EmployeeActionType.Resend, payload: { inviteId } });
    },
  })),
  withHooks({
    onInit(store) {
      let seen = store._actions.lastResult()?.sequence ?? 0;
      effect(() => {
        const result = store._actions.lastResult();
        if (result !== null && result.type === EmployeeActionType.Saved && result.sequence > seen) {
          seen = result.sequence;
          untracked(() => store.invites.reload());
        }
      });
    },
  }),
);
