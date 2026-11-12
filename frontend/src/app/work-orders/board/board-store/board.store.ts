import { computed, inject } from '@angular/core';
import { signalStore, withComputed, withMethods } from '@ngrx/signals';

import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';
import { withToasts } from '@shared/toasts/with-toasts';

import { withBoardColumns } from './board.columns.slice';
import { withBoardDrop } from './board.drop.slice';
import { withBoardFilter } from './board.filter.slice';
import { withBoardLayout } from './board.layout.slice';
import type { WorkOrderStatus } from '@work-orders/work-order';
import { INITIAL_RANGE } from './board.store.utils';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

/**
 * The status board: one lazy window per status column. The filter (title, priority, category, assignee, machine) comes
 * from the shared `WorkOrdersGridStore`; the planned start range is the own range of the board and defaults to today.
 * A drop moves the card only after the server accepted the change. Each slice owns one state group.
 */
export const BoardStore = signalStore(
  withToasts(),
  withBoardFilter(),
  withBoardLayout(),
  withBoardColumns(),
  withBoardDrop(),
  withComputed(() => {
    const auth = inject(AuthStore);
    return {
      canWrite: computed(() => auth.hasPermission(Permission.WorkOrdersWrite)),
    };
  }),
  withMethods((store) => ({
    /** Shows or hides a column. A column that becomes visible loads its first rows. */
    setColumnVisible(status: WorkOrderStatus, isVisible: boolean): void {
      if (store._applyVisibility(status, isVisible) && isVisible) {
        store._resetColumn(status);
        store._loadRanges([{ status, range: INITIAL_RANGE }]);
      }
    },
  })),
  withStoreDevtools('board'),
);
