import { Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { MenuItem } from 'primeng/api';
import { ButtonDirective } from 'primeng/button';
import { Menu } from 'primeng/menu';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';

import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';

import { type WorkOrderRow, type WorkOrderStatus } from '@work-orders/work-order';
import { PRIORITY_LABEL_KEYS, STATUS_LABEL_KEYS } from '@work-orders/list/work-order-grid/work-order-grid.utils';
import { statusTargets } from '@work-orders/board/board.store.utils';

const CHANGE_STATUS_KEY = 'WORK_ORDERS.BOARD.CHANGE_STATUS';

export interface StatusChoice {
  readonly row: WorkOrderRow;
  readonly target: WorkOrderStatus;
}

@Component({
  imports: [ButtonDirective, LocalDateTimePipe, Menu, Tag, Tooltip, TranslatePipe],
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
    this._translate.stream([
      ...Object.values(STATUS_LABEL_KEYS),
      ...Object.values(PRIORITY_LABEL_KEYS),
      CHANGE_STATUS_KEY,
    ]),
    { initialValue: {} as Record<string, string> },
  );

  protected readonly priorityLabel = computed(
    () => this._labels()[PRIORITY_LABEL_KEYS[this.row().priority]] ?? this.row().priority,
  );
  protected readonly targets = computed<MenuItem[]>(() =>
    statusTargets(this.row().status, this.canManage()).map((target) => ({
      id: target,
      label: this._labels()[STATUS_LABEL_KEYS[target]] ?? target,
      command: (): void => this.statusChosen.emit({ row: this.row(), target }),
    })),
  );
  protected readonly menuItems = computed<MenuItem[]>(() => [
    { label: this._labels()[CHANGE_STATUS_KEY] ?? CHANGE_STATUS_KEY, items: this.targets() },
  ]);
  protected readonly canChangeStatus = computed(() => this.canWrite() && this.targets().length > 0);
}
