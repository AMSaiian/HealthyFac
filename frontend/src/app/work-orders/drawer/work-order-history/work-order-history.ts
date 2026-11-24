import { Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Paginator, type PaginatorState } from 'primeng/paginator';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';

import { GRID_TOOLTIP_KEYS } from '@shared/grid/table/grid-tooltips';
import { toKeysetMove } from '@shared/grid/table/grid-paginator';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';

import { toHistoryEntries } from '@work-orders/drawer/work-order-history/work-order-history.utils';
import type { WorkOrderHistoryRow, WorkOrderStatus } from '@work-orders/work-order';
import { STATUS_LABEL_KEYS } from '@work-orders/list/work-order-grid/work-order-grid.utils';
import { ESTIMATE_SUFFIX, HISTORY_PAGE_SIZE } from '@work-orders/work-order-limits';

/**
 * One page of the history of a work order with a paginator that has previous and next only (a keyset page). The list
 * is the one vertical scroll boundary of the panel, and the paginator stays under it.
 */
@Component({
  imports: [LocalDateTimePipe, Paginator, Skeleton, Tag, TranslatePipe],
  host: { class: 'flex min-h-0 flex-1 flex-col gap-2' },
  selector: 'hf-work-order-history',
  templateUrl: './work-order-history.html',
})
export class WorkOrderHistory {
  readonly rows = input.required<readonly WorkOrderHistoryRow[]>();
  readonly totalCount = input(0);
  /** The 0-based page that the list shows. */
  readonly pageIndex = input(0);
  readonly isLoading = input(false);

  readonly nextRequested = output<void>();
  readonly previousRequested = output<void>();

  protected readonly showSkeleton = delayedLoading(this.isLoading);
  protected readonly pageSize = HISTORY_PAGE_SIZE;
  protected readonly estimateSuffix = ESTIMATE_SUFFIX;
  protected readonly newestFirst = computed(() => toHistoryEntries(this.rows()));
  protected readonly first = computed(() => this.pageIndex() * HISTORY_PAGE_SIZE);

  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([GRID_TOOLTIP_KEYS.previousPage, GRID_TOOLTIP_KEYS.nextPage]),
    { initialValue: {} as Record<string, string> },
  );
  protected readonly paginatorPt = computed(() => ({
    prev: { title: this._labels()[GRID_TOOLTIP_KEYS.previousPage] ?? GRID_TOOLTIP_KEYS.previousPage },
    next: { title: this._labels()[GRID_TOOLTIP_KEYS.nextPage] ?? GRID_TOOLTIP_KEYS.nextPage },
  }));

  protected statusKey(status: WorkOrderStatus): string {
    return STATUS_LABEL_KEYS[status];
  }

  protected onPage(event: PaginatorState): void {
    const move = toKeysetMove({
      page: event.page ?? 0,
      rows: event.rows ?? HISTORY_PAGE_SIZE,
      pageIndex: this.pageIndex(),
      limit: HISTORY_PAGE_SIZE,
    });
    if (move === 'next') {
      this.nextRequested.emit();
    } else if (move === 'previous') {
      this.previousRequested.emit();
    }
  }
}
