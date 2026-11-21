import { Component, computed, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tooltip } from 'primeng/tooltip';

import { DraftApplyDialog } from '@work-orders/drafts/draft-apply-dialog/draft-apply-dialog';
import type { WorkOrderDraftsPage as DraftsResult } from '@work-orders/drafts/work-order-draft';
import { WorkOrderDraftsGrid } from '@work-orders/drafts/work-order-drafts-grid/work-order-drafts-grid';
import { WorkOrderDraftsGridStore } from '@work-orders/drafts/work-order-drafts-grid.store';
import { WORK_ORDERS_RETURN_TARGET } from '@work-orders/work-orders-return-target';
import { HistoryStore } from '@shared/history/history.store';

const EMPTY_PAGE: DraftsResult = { items: [], totalCount: 0 };

@Component({
  imports: [ButtonDirective, DraftApplyDialog, Tooltip, TranslatePipe, WorkOrderDraftsGrid],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4' },
  selector: 'hf-work-order-drafts-page',
  templateUrl: './work-order-drafts-page.html',
})
export class WorkOrderDraftsPage {
  protected readonly store = inject(WorkOrderDraftsGridStore);
  protected readonly history = inject(HistoryStore);
  protected readonly returnTarget = WORK_ORDERS_RETURN_TARGET;

  protected readonly page = computed(() => (this.store.drafts.hasValue() ? this.store.drafts.value() : EMPTY_PAGE));
}
