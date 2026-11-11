import type { CdkDragDrop } from '@angular/cdk/drag-drop';
import {
  type CdkDrag,
  CdkDragPlaceholder,
  CdkDragPreview,
  CdkDropList,
  CdkDrag as DragDirective,
} from '@angular/cdk/drag-drop';
import {
  afterNextRender,
  Component,
  computed,
  DestroyRef,
  effect,
  ElementRef,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';
import { Scroller, type ScrollerLazyLoadEvent } from 'primeng/scroller';
import { Skeleton } from 'primeng/skeleton';

import { delayedLoading } from '@shared/loading/delayed-loading';
import { EmptyState } from '@shared/loading/empty-state/empty-state';

import { type WorkOrderRow, WorkOrderStatus } from '@work-orders/work-order';
import { canEnterColumn, type ColumnState, type ScrollRange, slotsOf } from '@work-orders/board/board.store.utils';
import { type StatusChoice, WorkOrderCard } from '@work-orders/board/work-order-card/work-order-card';

export const CARD_HEIGHT = 114;

export interface CardDrop {
  readonly row: WorkOrderRow;
  readonly target: WorkOrderStatus;
}

@Component({
  imports: [
    DragDirective,
    CdkDragPlaceholder,
    CdkDragPreview,
    CdkDropList,
    EmptyState,
    Scroller,
    Skeleton,
    WorkOrderCard,
  ],
  host: { class: 'block h-full min-h-0 w-72 min-w-0 flex-none' },
  selector: 'hf-board-column',
  templateUrl: './board-column.html',
})
export class BoardColumn {
  readonly status = input.required<WorkOrderStatus>();
  readonly column = input.required<ColumnState>();
  readonly canManage = input(false);
  readonly canWrite = input(false);

  readonly rangeRequested = output<ScrollRange>();
  readonly dropped = output<CardDrop>();
  readonly opened = output<string>();
  readonly statusChosen = output<StatusChoice>();

  protected readonly cardHeight = CARD_HEIGHT;
  protected readonly isEmpty = computed(() => this.column().window.totalCount === 0 && !this.column().isLoading);
  protected readonly showSkeleton = delayedLoading(computed(() => this.column().isLoading));

  private readonly _host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _scroller = viewChild(Scroller);
  protected readonly slots = computed(() => slotsOf(this.column().window));
  protected readonly isDisabled = computed(
    () => this.column().isLoading || (this.status() === WorkOrderStatus.Backlog && !this.canManage()),
  );

  protected readonly canEnter = (drag: CdkDrag<WorkOrderRow>): boolean =>
    canEnterColumn(drag.data.status, this.status(), this.canManage(), this.column().isLoading);

  constructor() {
    effect(() => {
      void this.slots();
      this._scroller()?.init();
    });
    afterNextRender(() => {
      const observer = new ResizeObserver(() => this._scroller()?.init());
      observer.observe(this._host.nativeElement);
      this._destroyRef.onDestroy(() => observer.disconnect());
    });
  }

  protected onLazyLoad(event: ScrollerLazyLoadEvent): void {
    this.rangeRequested.emit({ first: event.first, last: event.last });
  }

  protected onDrop(event: CdkDragDrop<unknown, unknown, WorkOrderRow>): void {
    if (event.previousContainer !== event.container) {
      this.dropped.emit({ row: event.item.data, target: this.status() });
    }
  }
}
