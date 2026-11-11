import { Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';

import { delayedLoading } from '@shared/loading/delayed-loading';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';

import type { WorkOrderHistoryRow, WorkOrderStatus } from '@work-orders/work-order';
import { STATUS_LABEL_KEYS } from '@work-orders/list/work-order-grid/work-order-grid.utils';

@Component({
  imports: [LocalDateTimePipe, Skeleton, Tag, TranslatePipe],
  host: { class: 'flex flex-col gap-2' },
  selector: 'hf-work-order-history',
  templateUrl: './work-order-history.html',
})
export class WorkOrderHistory {
  readonly rows = input.required<readonly WorkOrderHistoryRow[]>();
  readonly isLoading = input(false);
  protected readonly showSkeleton = delayedLoading(this.isLoading);

  protected readonly newestFirst = computed(() => [...this.rows()].sort((a, b) => b.revision - a.revision));

  protected statusKey(status: WorkOrderStatus): string {
    return STATUS_LABEL_KEYS[status];
  }
}
