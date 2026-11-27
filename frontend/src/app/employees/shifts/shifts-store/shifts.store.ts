import { computed, inject } from '@angular/core';
import { signalStore, withComputed, withMethods } from '@ngrx/signals';

import { ActionStore } from '@shared/actions/action.store';
import { withConfirm } from '@shared/confirm/with-confirm';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';
import { withToasts } from '@shared/toasts/with-toasts';

import type { ShiftOverride, ShiftVersion } from '@employees/shifts/shift';
import { isEditableOverride, isEditableVersion } from '@employees/shifts/shifts-store/shifts.store.utils';
import { withShiftsDialog } from '@employees/shifts/shifts-store/shifts.dialog.slice';
import { withShiftsPrefill } from '@employees/shifts/shifts-store/shifts.prefill.slice';
import { withShiftsSelection } from '@employees/shifts/shifts-store/shifts.selection.slice';
import { WorkOrderActionType } from '@work-orders/work-order-actions';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

/**
 * The versions and the overrides of the shifts of one employee. A supervisor changes them; a worker reads them. Each
 * slice owns one state group: the selection, or the open dialog with its save in progress.
 */
export const ShiftsStore = signalStore(
  withToasts(),
  withConfirm(),
  withShiftsSelection(),
  withShiftsDialog(),
  withShiftsPrefill(),
  withComputed(() => {
    const auth = inject(AuthStore);
    return {
      canManage: computed(() => auth.hasPermission(Permission.ShiftsWriteCompany)),
    };
  }),
  withMethods((store) => {
    const actions = inject(ActionStore);
    return {
      isEditable(version: ShiftVersion): boolean {
        return store.canManage() && isEditableVersion(version, store.today());
      },
      isOverrideEditable(override: ShiftOverride): boolean {
        return store.canManage() && isEditableOverride(override, store.today());
      },
      openWorkOrder(id: string): void {
        void actions.dispatch({ type: WorkOrderActionType.Open, payload: { workOrderId: id } });
      },
    };
  }),
  withStoreDevtools('shifts'),
);
