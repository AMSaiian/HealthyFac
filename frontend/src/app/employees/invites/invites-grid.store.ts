import { inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withMethods, withProps, withState } from '@ngrx/signals';

import { ActionStore } from '@shared/actions/action.store';
import { type Reloadable, withActionResultReload } from '@shared/actions/with-action-result-reload';
import { withConfirm } from '@shared/confirm/with-confirm';
import { withGridState } from '@shared/grid/with-grid-state';

import type { InviteColumn, InviteFilter, InviteRow, InviteSortColumn } from '@employees/employee';
import { EmployeeActionType } from '@employees/employee-actions';
import { EmployeesApi } from '@employees/employees-api';
import { INITIAL_GRID, toSearchBody } from '@employees/invites/invites-grid.store.utils';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

/** The grid state and the rows of the pending invites. */
export const InvitesGridStore = signalStore(
  withGridState<InviteFilter, InviteSortColumn, InviteColumn>(INITIAL_GRID),
  withState({ isActive: false }),
  withProps(() => ({
    _actions: inject(ActionStore),
    _api: inject(EmployeesApi),
  })),
  withConfirm(),
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
    /**
     * Starts the loading on the first call, so a closed tab sends no request.
     * Each later call reloads the rows, so a return to the tab shows the current invites.
     */
    activate(): void {
      if (store.isActive()) {
        store.invites.reload();
      } else {
        patchState(store, { isActive: true });
      }
    },
    /** Asks to confirm, then starts the revoke action with the row version that the row showed. */
    requestRevoke(row: InviteRow): void {
      store._confirmDestructive({
        headerKey: 'EMPLOYEES.REVOKE_HEADER',
        messageKey: 'EMPLOYEES.REVOKE_MESSAGE',
        params: { email: row.email },
        acceptKey: 'EMPLOYEES.REVOKE',
        onAccept: () => {
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
  withActionResultReload<{ invites: Reloadable }>([EmployeeActionType.Saved], (store) => {
    store.invites.reload();
  }),
  withStoreDevtools('invites-grid'),
);
