import { signalStore } from '@ngrx/signals';

import { withDetailsMode } from '@shared/forms/with-details-mode';
import { withToasts } from '@shared/toasts/with-toasts';

import { withAssignmentLookups } from '@work-orders/assignment-picker/with-assignment-lookups';
import { withWorkOrderDrawerAction } from './work-order-drawer.action.slice';
import { withWorkOrderDrawerForm } from './work-order-drawer.form.slice';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

/**
 * Holds the work order drawer. Each slice owns one state group: the open action with its history and its save, the
 * lookups of the assignment picker, or the form with its uploads.
 */
export const WorkOrderDrawerStore = signalStore(
  withToasts(),
  withDetailsMode(),
  withWorkOrderDrawerAction(),
  withAssignmentLookups(),
  withWorkOrderDrawerForm(),
  withStoreDevtools('work-order-drawer'),
);
