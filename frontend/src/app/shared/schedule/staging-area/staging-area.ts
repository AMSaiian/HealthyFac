import { CdkDrag, type CdkDragEnd } from '@angular/cdk/drag-drop';
import { Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Tooltip } from 'primeng/tooltip';

import type { Axis } from '@shared/schedule/schedule-board/schedule-board.utils';
import {
  LANE_HEIGHT_REM,
  NAME_COLUMN_REM,
  ROW_PADDING_REM,
} from '@shared/schedule/schedule-board/schedule-board.utils';
import {
  packLanes,
  stagingBarTopRem,
  stagingHeightRem,
  type StagingItem,
} from '@shared/schedule/staging-area/staging-area.utils';

export interface StagingRelease {
  readonly workOrderId: string;
  /** The left edge of the dragged bar in client pixels. */
  readonly barLeftPx: number;
  readonly pointer: { readonly x: number; readonly y: number };
}

/**
 * The backlog work orders of the schedule tab as bars as wide as their estimate on the axis scale. A bar drags to a
 * row of the board; the board reports a row bar that lands here. The area only reports the release of a bar.
 */
@Component({
  imports: [CdkDrag, Tooltip, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-col', 'data-staging': '' },
  selector: 'hf-staging-area',
  templateUrl: './staging-area.html',
})
export class StagingArea {
  readonly workOrders = input.required<readonly StagingItem[]>();
  readonly totalCount = input(0);
  readonly axis = input.required<Axis>();

  readonly barReleased = output<StagingRelease>();
  readonly workOrderClick = output<string>();

  protected readonly nameColumnRem = NAME_COLUMN_REM;
  protected readonly rowPaddingRem = ROW_PADDING_REM;
  protected readonly laneHeightRem = LANE_HEIGHT_REM;
  protected readonly barTopRem = stagingBarTopRem;
  protected readonly heightRem = stagingHeightRem;
  protected readonly layout = computed(() => packLanes(this.workOrders(), this.axis()));
  protected readonly isMoreAvailable = computed(() => this.totalCount() > this.workOrders().length);

  protected onDragEnded(event: CdkDragEnd, id: string): void {
    const rect = event.source.element.nativeElement.getBoundingClientRect();
    event.source.reset();
    this.barReleased.emit({ workOrderId: id, barLeftPx: rect.left, pointer: event.dropPoint });
  }
}
