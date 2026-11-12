import { computed } from '@angular/core';
import { patchState, signalStoreFeature, withComputed, withMethods, withState } from '@ngrx/signals';

import { type GridColumnState, reorderColumns, toggleColumn, visibleColumns } from '@shared/grid/table/grid-columns';

import type { WorkOrderStatus } from '@work-orders/work-order';
import { BOARD_STATUSES } from './board.store.utils';

interface LayoutState {
  layout: readonly GridColumnState<WorkOrderStatus>[];
}

/** The layout of the columns: their order and which of them are visible. */
export function withBoardLayout() {
  return signalStoreFeature(
    withState<LayoutState>({ layout: BOARD_STATUSES.map((column) => ({ column, isVisible: true })) }),
    withComputed((store) => ({
      visibleStatuses: computed(() => visibleColumns(store.layout())),
    })),
    withMethods((store) => ({
      /** Shows or hides a column. The last visible column stays. Returns `true` when the layout changed. */
      _applyVisibility(status: WorkOrderStatus, isVisible: boolean): boolean {
        const visible = store.visibleStatuses();
        if (visible.includes(status) === isVisible || (!isVisible && visible.length <= 1)) {
          return false;
        }
        const flagged = store.layout().map((entry) => (entry.column === status ? { ...entry, isVisible } : entry));
        patchState(store, { layout: reorderColumns(flagged, toggleColumn(visible, status, isVisible)) });
        return true;
      },
      /** Puts the visible columns in a new order. A hidden column keeps its slot. */
      reorder(order: readonly WorkOrderStatus[]): void {
        patchState(store, { layout: reorderColumns(store.layout(), order) });
      },
    })),
  );
}
