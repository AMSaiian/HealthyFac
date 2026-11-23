import { CDK_DRAG_CONFIG, CdkDrag, type CdkDragEnd, CdkDragPreview, CdkDropList } from '@angular/cdk/drag-drop';
import { Component, computed, inject, input, output, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Tooltip } from 'primeng/tooltip';

import {
  type Axis,
  createClickGuard,
  DRAG_PREVIEW_Z_INDEX,
  LANE_HEIGHT_REM,
} from '@shared/schedule/schedule-board/schedule-board.utils';
import {
  chipTooltipOf,
  estimateTextOf,
  previewWidthOf,
  type StagingItem,
} from '@shared/schedule/staging-area/staging-area.utils';

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
  readonly axis = input.required<Axis>();
  /** The width of the board axis in pixels. It gives the width of a drag preview. */
  readonly axisWidthPx = input(0);

  readonly dragStarted = output<void>();
  readonly dragMoved = output<{ readonly x: number; readonly y: number }>();
  readonly barReleased = output<StagingRelease>();
  readonly workOrderClick = output<string>();

  protected readonly laneHeightRem = LANE_HEIGHT_REM;
  protected readonly isMoreAvailable = computed(() => this.totalCount() > this.workOrders().length);
  protected readonly cards = computed(() =>
    this.workOrders().map((item) => ({
      item,
      tooltip: this._tooltipOf(item),
      previewWidthPx: previewWidthOf(item.estimateMinutes, this.axis(), this.axisWidthPx()),
    })),
  );

  private readonly _translate = inject(TranslateService);
  protected readonly isDragging = signal(false);

  private readonly _clickGuard = createClickGuard();

  protected onDragStarted(): void {
    this.isDragging.set(true);
    this.dragStarted.emit();
  }

  protected onDragEnded(event: CdkDragEnd, id: string): void {
    this.isDragging.set(false);
    this._clickGuard.markDragged();
    const card = this.cards().find((entry) => entry.item.id === id);
    const previewLeftPx = event.dropPoint.x - (card?.previewWidthPx ?? 0) / 2;
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
