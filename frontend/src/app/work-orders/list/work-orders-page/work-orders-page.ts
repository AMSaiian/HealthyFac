import { Component, computed, DestroyRef, inject } from '@angular/core';

import type { WorkOrdersPage as WorkOrdersResult } from '@work-orders/work-order';
import { WorkOrderGrid } from '@work-orders/list/work-order-grid/work-order-grid';
import { WorkOrdersGridStore } from '@work-orders/list/work-orders-grid.store';

const EMPTY_PAGE: WorkOrdersResult = { items: [], totalCount: 0 };

import { ViewHeader } from '@work-orders/view-header/view-header';

@Component({
  imports: [ViewHeader, WorkOrderGrid],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4' },
  selector: 'hf-work-orders-page',
  templateUrl: './work-orders-page.html',
})
export class WorkOrdersPage {
  protected readonly store = inject(WorkOrdersGridStore);

  constructor() {
    this.store.setListShown(true);
    inject(DestroyRef).onDestroy(() => this.store.setListShown(false));
  }

  protected readonly page = computed(() =>
    this.store.workOrders.hasValue() ? this.store.workOrders.value() : EMPTY_PAGE,
  );
}
