import { Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Listbox } from 'primeng/listbox';
import { Popover } from 'primeng/popover';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';

import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';

import { type WorkOrderRow, type WorkOrderStatus } from '@work-orders/work-order';
import { PRIORITY_LABEL_KEYS, STATUS_LABEL_KEYS } from '@work-orders/list/work-order-grid/work-order-grid.utils';
import { statusTargets } from '@work-orders/board/board-store/board.store.utils';

export interface StatusChoice {
  readonly row: WorkOrderRow;
  readonly target: WorkOrderStatus;
}

@Component({
  imports: [ButtonDirective, LocalDateTimePipe, Listbox, Popover, Tag, Tooltip, TranslatePipe],
  host: { class: 'block h-full min-w-0' },
  selector: 'hf-work-order-card',
  templateUrl: './work-order-card.html',
})
export class WorkOrderCard {
  readonly row = input.required<WorkOrderRow>();
  readonly canManage = input(false);
  readonly canWrite = input(false);

  readonly opened = output<string>();
  readonly statusChosen = output<StatusChoice>();

  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([...Object.values(STATUS_LABEL_KEYS), ...Object.values(PRIORITY_LABEL_KEYS)]),
    { initialValue: {} as Record<string, string> },
  );

  protected readonly priorityLabel = computed(
    () => this._labels()[PRIORITY_LABEL_KEYS[this.row().priority]] ?? this.row().priority,
  );
  protected readonly targets = computed<WorkOrderStatus[]>(() => [
    ...statusTargets(this.row().status, this.canManage()),
  ]);
  protected readonly canChangeStatus = computed(() => this.canWrite() && this.targets().length > 0);

  protected statusLabel(status: WorkOrderStatus): string {
    return this._labels()[STATUS_LABEL_KEYS[status]] ?? STATUS_LABEL_KEYS[status];
  }

  protected choose(target: WorkOrderStatus): void {
    this.statusChosen.emit({ row: this.row(), target });
  }
}
