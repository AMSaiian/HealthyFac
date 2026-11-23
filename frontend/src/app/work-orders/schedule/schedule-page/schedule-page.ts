import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Drawer } from 'primeng/drawer';
import { Message } from 'primeng/message';
import { Tooltip } from 'primeng/tooltip';

import { ScheduleBoard } from '@shared/schedule/schedule-board/schedule-board';
import { axisOf } from '@shared/schedule/schedule-board/schedule-board.utils';
import { type PreviewAt, StagingArea, type StagingRelease } from '@shared/schedule/staging-area/staging-area';
import { CLOCK } from '@shared/time/clock';
import { PlainDatePicker } from '@shared/time/pickers/plain-date-picker/plain-date-picker';
import type { Temporal } from '@shared/time/temporal';

import { WorkOrdersGridStore } from '@work-orders/list/work-orders-grid.store';
import { ScheduleStore } from '@work-orders/schedule/schedule-store/schedule.store';
import { ViewHeader } from '@work-orders/view-header/view-header';
import {
  BACKLOG_DRAWER_CLASS,
  BACKLOG_DRAWER_REM,
  BACKLOG_TOGGLE_ATTRIBUTE,
  backlogDrawerStateOf,
  backlogPointerActionOf,
  isBacklogOpenAfterDrag,
  isReturnBandShown,
  stagingCardOf,
} from './schedule-page.utils';

@Component({
  imports: [
    ButtonDirective,
    Drawer,
    Message,
    PlainDatePicker,
    ScheduleBoard,
    StagingArea,
    Tooltip,
    TranslatePipe,
    ViewHeader,
  ],
  host: {
    class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4',
    '(document:pointerdown)': 'onDocumentPointerDown($event)',
  },
  selector: 'hf-schedule-page',
  templateUrl: './schedule-page.html',
})
export class SchedulePage {
  protected readonly store = inject(ScheduleStore);
  protected readonly grid = inject(WorkOrdersGridStore);
  protected readonly clock = inject(CLOCK);
  /** Places the drawer inside the content region, so it covers neither the shell header nor the board hours. */
  protected readonly drawerStyle = {
    position: 'absolute',
    top: '0',
    right: '0',
    bottom: '0',
    height: 'auto',
    width: `${BACKLOG_DRAWER_REM}rem`,
  };
  protected readonly isStagingDragActive = signal(false);
  protected readonly isClosingQuietly = signal(false);
  protected readonly drawerClass = BACKLOG_DRAWER_CLASS;
  protected readonly isBoardDragActive = signal(false);

  protected readonly axis = computed(() => {
    const date = this.store.date();
    return date === null ? null : axisOf(date, this.clock.timeZoneId());
  });
  protected readonly failures = computed(() => new Map(Object.entries(this.store.failures())));
  protected readonly stagingCards = computed(() => this.store.stagingItems().map(stagingCardOf));
  protected readonly drawerState = computed(() =>
    backlogDrawerStateOf(this.store.isBacklogOpen(), this.isStagingDragActive(), this.isClosingQuietly()),
  );
  protected readonly isReturnBandVisible = computed(() =>
    isReturnBandShown(this.isBoardDragActive(), this.store.canManage()),
  );

  private readonly _board = viewChild(ScheduleBoard);
  protected readonly axisWidthPx = computed(() => this._board()?.axisWidthPx() ?? 0);
  protected readonly previewAt: PreviewAt = (pointer, grabPx, estimateMinutes, excludedId) =>
    this._board()?.previewAt(pointer, grabPx, estimateMinutes, excludedId) ?? { slot: null, point: pointer };

  protected onDocumentPointerDown(event: PointerEvent): void {
    const target = event.target;
    if (!this.store.isBacklogOpen() || !(target instanceof Element)) {
      return;
    }
    const action = backlogPointerActionOf(
      target.closest(`.${BACKLOG_DRAWER_CLASS}`) !== null,
      target.closest(`[${BACKLOG_TOGGLE_ATTRIBUTE}]`) !== null,
    );
    if (action === 'close') {
      this.store.setBacklogOpen(false);
    }
  }

  protected onDrawerShown(): void {
    this.isClosingQuietly.set(false);
  }

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

  protected onStagingDragStarted(): void {
    this.isStagingDragActive.set(true);
    this.store.setBacklogOpen(isBacklogOpenAfterDrag(this.store.isBacklogOpen(), 'backlogDragStarted'));
  }

  protected onBoardDragActive(isActive: boolean): void {
    this.isBoardDragActive.set(isActive);
    this.store.setBacklogOpen(
      isBacklogOpenAfterDrag(this.store.isBacklogOpen(), isActive ? 'boardDragStarted' : 'boardDragEnded'),
    );
  }

  protected onStagingMove(pointer: { readonly x: number; readonly y: number }): void {
    this._board()?.trackExternalDrag(pointer);
  }

  protected onStagingRelease(release: StagingRelease): void {
    this.isStagingDragActive.set(false);
    this.isClosingQuietly.set(!this.store.isBacklogOpen());
    this._board()?.placeExternal(release.workOrderId, release.previewLeftPx, release.pointer);
  }
}
