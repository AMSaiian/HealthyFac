import { Component, computed, inject, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Splitter, SplitterPanel } from 'primeng/splitter';
import { Tooltip } from 'primeng/tooltip';

import { ScheduleBoard } from '@shared/schedule/schedule-board/schedule-board';
import { axisOf } from '@shared/schedule/schedule-board/schedule-board.utils';
import { StagingArea, type StagingRelease } from '@shared/schedule/staging-area/staging-area';
import { CLOCK } from '@shared/time/clock';
import { PlainDatePicker } from '@shared/time/pickers/plain-date-picker/plain-date-picker';
import type { Temporal } from '@shared/time/temporal';

import { WorkOrdersGridStore } from '@work-orders/list/work-orders-grid.store';
import { ScheduleStore } from '@work-orders/schedule/schedule-store/schedule.store';
import { ViewHeader } from '@work-orders/view-header/view-header';

const STAGING_PANEL_SIZE = 25;
const BOARD_PANEL_SIZE = 75;
const MIN_PANEL_SIZE = 10;

@Component({
  imports: [
    ButtonDirective,
    PlainDatePicker,
    ScheduleBoard,
    Splitter,
    SplitterPanel,
    StagingArea,
    Tooltip,
    TranslatePipe,
    ViewHeader,
  ],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4' },
  selector: 'hf-schedule-page',
  templateUrl: './schedule-page.html',
})
export class SchedulePage {
  protected readonly store = inject(ScheduleStore);
  protected readonly grid = inject(WorkOrdersGridStore);
  protected readonly clock = inject(CLOCK);
  protected readonly stagingSize = STAGING_PANEL_SIZE;
  protected readonly boardSize = BOARD_PANEL_SIZE;
  protected readonly minSize = MIN_PANEL_SIZE;

  protected readonly axis = computed(() => {
    const date = this.store.date();
    return date === null ? null : axisOf(date, this.clock.timeZoneId());
  });
  protected readonly failures = computed(() => new Map(Object.entries(this.store.failures())));

  private readonly _board = viewChild(ScheduleBoard);

  protected onDate(date: Temporal.PlainDate | null): void {
    if (date !== null) {
      this.store.setDate(date);
    }
  }

  protected onDaySwitch(direction: -1 | 1): void {
    if (direction === -1) {
      this.store.previousDay();
    } else {
      this.store.nextDay();
    }
  }

  protected onStagingRelease(release: StagingRelease): void {
    this._board()?.placeExternal(release.workOrderId, release.barLeftPx, release.pointer);
  }
}
