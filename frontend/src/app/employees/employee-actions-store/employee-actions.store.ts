import { signalStore, withMethods } from '@ngrx/signals';

import type { AppAction } from '@shared/actions/app-action';
import { withToasts } from '@shared/toasts/with-toasts';

import { withEmployeeActionsAction } from './employee-actions.action.slice';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

/**
 * Runs the actions of the employees page: the invite dialog, the resend of an invite, and the retry of the
 * provisioning. A finished action completes with `employees.saved`, so the grids reload. The action slice owns the open
 * action and the write in progress.
 */
export const EmployeeActionsStore = signalStore(
  withToasts(),
  withEmployeeActionsAction(),
  withMethods((store) => ({
    start(action: AppAction): void {
      store._setAction(action);
      store._runAction(action);
    },
  })),
  withStoreDevtools('employee-actions'),
);
