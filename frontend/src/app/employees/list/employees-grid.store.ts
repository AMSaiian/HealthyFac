import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { signalStore, withComputed, withMethods, withProps } from '@ngrx/signals';

import { ActionStore } from '@shared/actions/action.store';
import { type Reloadable, withActionResultReload } from '@shared/actions/with-action-result-reload';
import { withGridState } from '@shared/grid/with-grid-state';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';

import type { EmployeeColumn, EmployeeFilter, EmployeeSortColumn } from '@employees/employee';
import { EmployeeActionType } from '@employees/employee-actions';
import { EmployeesApi } from '@employees/employees-api';
import { INITIAL_GRID, toSearchBody } from '@employees/list/employees-grid.store.utils';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

const EMPLOYEES_PATH = '/employees';
const SHIFTS_SEGMENT = 'shifts';
const SCHEDULE_SEGMENT = 'schedule';

/** The grid state and the rows of the employees page. */
export const EmployeesGridStore = signalStore(
  withGridState<EmployeeFilter, EmployeeSortColumn, EmployeeColumn>(INITIAL_GRID),
  withProps(() => ({
    _auth: inject(AuthStore),
    _actions: inject(ActionStore),
    _api: inject(EmployeesApi),
    _router: inject(Router),
  })),
  withComputed((store) => ({
    canInvite: computed(() => store._auth.hasPermission(Permission.EmployeesInvite)),
    canManage: computed(() => store._auth.hasPermission(Permission.EmployeesManage)),
    canReadShifts: computed(() => store._auth.hasPermission(Permission.ShiftsRead)),
  })),
  withProps((store) => ({
    employees: rxResource({
      params: () =>
        toSearchBody({ offset: store.offset(), limit: store.limit(), sort: store.sort(), filter: store.filter() }),
      stream: ({ params }) => store._api.searchEmployees(params),
    }),
  })),
  withMethods((store) => ({
    /** Reloads the rows, so a return to the tab shows the current employees. */
    refresh(): void {
      store.employees.reload();
    },
    requestInvite(): void {
      void store._actions.dispatch({ type: EmployeeActionType.Invite, payload: {} });
    },
    requestRetry(employeeId: string): void {
      void store._actions.dispatch({ type: EmployeeActionType.RetryProvisioning, payload: { employeeId } });
    },
    openShifts(employeeId: string): void {
      void store._router.navigate([EMPLOYEES_PATH, employeeId, SHIFTS_SEGMENT]);
    },
    openSchedule(employeeId: string): void {
      void store._router.navigate([EMPLOYEES_PATH, employeeId, SCHEDULE_SEGMENT]);
    },
  })),
  withActionResultReload<{ employees: Reloadable }>([EmployeeActionType.Saved], (store) => {
    store.employees.reload();
  }),
  withStoreDevtools('employees-grid'),
);
