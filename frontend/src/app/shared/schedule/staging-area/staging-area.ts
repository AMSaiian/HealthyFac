import { CDK_DRAG_CONFIG, CdkDrag, type CdkDragEnd, CdkDragPreview, CdkDropList } from '@angular/cdk/drag-drop';
import { Component, computed, inject, input, output, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Tooltip } from 'primeng/tooltip';

import {
  type Axis,
  createClickGuard,
  DRAG_PREVIEW_Z_INDEX,
  LANE_HEIGHT_REM,
  type Point,
  type PreviewDrag,
  type PreviewSlot,
} from '@shared/schedule/schedule-board/schedule-board.utils';
import {
  chipTooltipOf,
  estimateTextOf,
  previewWidthOf,
  type StagingItem,
} from '@shared/schedule/staging-area/staging-area.utils';

/** Gives the slot that a preview takes under the pointer: the pointer, the grab offset, the estimate, and the bar to ignore. */
export type PreviewAt = (
  pointer: Point,
  grabPx: number,
  estimateMinutes: number,
  excludedId: string | null,
) => PreviewDrag;

export interface StagingRelease {
  readonly workOrderId: string;
  /** The left edge of the drag preview in client pixels. */
  readonly previewLeftPx: number;
  readonly pointer: { readonly x: number; readonly y: number };
}

/**
 * The backlog work orders of the schedule tab as cards. A card drags to a row of the board with a preview as wide as
 * its estimate on the board axis. The board reports a row bar that lands here. The area only reports the drag.
 */
@Component({
  imports: [CdkDrag, CdkDragPreview, CdkDropList, Tooltip, TranslatePipe],
  host: { class: 'flex min-w-0 flex-col' },
  providers: [{ provide: CDK_DRAG_CONFIG, useValue: { zIndex: DRAG_PREVIEW_Z_INDEX } }],
  selector: 'hf-staging-area',
  templateUrl: './staging-area.html',
})
export class StagingArea {
  readonly workOrders = input.required<readonly StagingItem[]>();
  readonly totalCount = input(0);
  /** Whether the server holds more backlog items than the list shows. */
  readonly isCut = input(false);
  readonly axis = input.required<Axis>();
  /** The width of the board axis in pixels. It gives the width of a drag preview. */
  readonly axisWidthPx = input(0);
  /** Snaps a preview to the board. The page gives the function of the board. */
  readonly previewAt = input<PreviewAt>(() => ({ slot: null, point: { x: 0, y: 0 } }));

  readonly dragStarted = output<void>();
  readonly dragMoved = output<{ readonly x: number; readonly y: number }>();
  readonly barReleased = output<StagingRelease>();
  readonly workOrderClick = output<string>();

  protected readonly laneHeightRem = LANE_HEIGHT_REM;
  protected readonly cards = computed(() =>
    this.workOrders().map((item) => ({
      item,
      tooltip: this._tooltipOf(item),
      previewWidthPx: previewWidthOf(item.estimateMinutes, this.axis(), this.axisWidthPx()),
    })),
  );

  private readonly _translate = inject(TranslateService);
  protected readonly isDragging = signal(false);
  /** The snapped slot under the preview of the running drag, or `null` outside every row. */
  protected readonly previewSlot = signal<PreviewSlot | null>(null);
  protected readonly grabOffsetPx = signal(0);

  /** The point that the CDK takes as the pointer of a chip: on the snapped slot while the pointer is over a row. */
  protected readonly constrain = (point: Point): Point => {
    const holding = this._holding;
    return holding === null ? point : this.previewAt()(point, this.grabOffsetPx(), holding.estimateMinutes, null).point;
  };

  private readonly _clickGuard = createClickGuard();
  private _holding: { readonly estimateMinutes: number } | null = null;
  protected onPointerDown(event: PointerEvent, item: StagingItem): void {
    this._holding = { estimateMinutes: item.estimateMinutes };
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    this.grabOffsetPx.set(Math.min(rect.width, Math.max(0, event.clientX - rect.left)));
  }

  protected onDragMoved(pointer: Point): void {
    this.previewSlot.set(
      this.previewAt()(pointer, this.grabOffsetPx(), this._holding?.estimateMinutes ?? 0, null).slot,
    );
    this.dragMoved.emit(pointer);
  }

  protected onDragStarted(): void {
    this.isDragging.set(true);
    this.dragStarted.emit();
  }

  protected onDragEnded(event: CdkDragEnd, id: string): void {
    this.isDragging.set(false);
    this.previewSlot.set(null);
    this._clickGuard.markDragged();
    const previewLeftPx = event.dropPoint.x - this.grabOffsetPx();
    this.barReleased.emit({ workOrderId: id, previewLeftPx, pointer: event.dropPoint });
  }

  protected onClick(id: string): void {
    if (!this._clickGuard.isBlocked()) {
      this.workOrderClick.emit(id);
    }
  }

  private _tooltipOf(item: StagingItem): string {
    const estimate = estimateTextOf(item.estimateMinutes);
    return chipTooltipOf(item.title, this._translate.instant(estimate.key, estimate.params));
  }
}
