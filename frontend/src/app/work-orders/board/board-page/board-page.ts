import { CdkDrag, type CdkDragDrop, CdkDropList, CdkDropListGroup, moveItemInArray } from '@angular/cdk/drag-drop';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { Popover } from 'primeng/popover';
import { Select } from 'primeng/select';
import { Tag } from 'primeng/tag';

import { ChipSummary } from '@shared/chips/chip-summary/chip-summary';
import { type Chip, toChips } from '@shared/chips/chip-summary/chip-summary.utils';
import { PlainDateRangePicker } from '@shared/time/pickers/plain-date-range-picker/plain-date-range-picker';
import type { PlainDateRange } from '@shared/time/plain-date-range';

import { BOARD_STATUSES } from '@work-orders/board/board.store.utils';
import { BoardStore } from '@work-orders/board/board.store';
import { BoardColumn, type CardDrop } from '@work-orders/board/board-column/board-column';
import { ViewHeader } from '@work-orders/view-header/view-header';
import type { StatusChoice } from '@work-orders/board/work-order-card/work-order-card';
import { WorkOrderPriority } from '@work-orders/work-order';
import { WorkOrdersGridStore } from '@work-orders/list/work-orders-grid.store';
import { PRIORITY_LABEL_KEYS, STATUS_LABEL_KEYS } from '@work-orders/list/work-order-grid/work-order-grid.utils';

@Component({
  imports: [
    BoardColumn,
    ButtonDirective,
    CdkDrag,
    CdkDropList,
    CdkDropListGroup,
    Checkbox,
    ChipSummary,
    FormsModule,
    InputText,
    PlainDateRangePicker,
    Popover,
    Select,
    Tag,
    TranslatePipe,
    ViewHeader,
  ],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4' },
  selector: 'hf-board-page',
  templateUrl: './board-page.html',
})
export class BoardPage {
  protected readonly store = inject(BoardStore);
  protected readonly grid = inject(WorkOrdersGridStore);
  protected readonly statuses = BOARD_STATUSES;
  protected readonly statusLabelKeys = STATUS_LABEL_KEYS;

  protected readonly priorityOptions = Object.values(WorkOrderPriority).map((value) => ({
    value,
    labelKey: PRIORITY_LABEL_KEYS[value],
  }));

  private readonly _labels = toSignal(inject(TranslateService).stream(Object.values(PRIORITY_LABEL_KEYS)), {
    initialValue: {} as Record<string, string>,
  });

  protected priorityChips(priorities: readonly WorkOrderPriority[]): Chip[] {
    return toChips(priorities, 'hf-priority-', (priority) => this._labels()[PRIORITY_LABEL_KEYS[priority]] ?? '');
  }

  protected priorityLabelKey(priority: WorkOrderPriority): string {
    return PRIORITY_LABEL_KEYS[priority];
  }

  protected onHeaderDrop(event: CdkDragDrop<unknown>): void {
    const order = [...this.store.visibleStatuses()];
    moveItemInArray(order, event.previousIndex, event.currentIndex);
    this.store.reorder(order);
  }

  protected onDrop(drop: CardDrop): void {
    this.store.drop(drop.row, drop.target);
  }

  protected onStatusChosen(choice: StatusChoice): void {
    this.store.drop(choice.row, choice.target);
  }

  protected onRange(range: PlainDateRange): void {
    this.store.setPlannedStart(range);
  }
}
