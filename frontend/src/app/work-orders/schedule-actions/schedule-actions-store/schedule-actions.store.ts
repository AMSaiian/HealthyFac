import { signalStore } from '@ngrx/signals';

import { withConfirm } from '@shared/confirm/with-confirm';
import { withToasts } from '@shared/toasts/with-toasts';

import { withScheduleActionsAction } from './schedule-actions.action.slice';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

/**
 * Runs the row actions of the work orders (assign, move, unassign, change status, delete). A change completes the
 * action with `work-orders.saved`; a `409` completes it with `work-orders.conflict` after a toast, so the grid reloads;
 * a `422` shows the rule toast and keeps a dialog open. The action slice owns the open action and the save in progress.
 */
export const ScheduleActionsStore = signalStore(
  withToasts(),
  withConfirm(),
  withScheduleActionsAction(),
  withStoreDevtools('schedule-actions'),
);
