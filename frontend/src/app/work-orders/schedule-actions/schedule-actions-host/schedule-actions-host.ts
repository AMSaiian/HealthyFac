import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { Select } from 'primeng/select';
import { Tag } from 'primeng/tag';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import type { AppAction } from '@shared/actions/app-action';
import { LoadingSpinner } from '@shared/loading/loading-spinner/loading-spinner';

import type { WorkOrderStatus } from '@work-orders/work-order';
import { WorkOrderActionType } from '@work-orders/work-order-actions';
import { STATUS_LABEL_KEYS } from '@work-orders/list/work-order-grid/work-order-grid.utils';
import { WorkOrdersApi } from '@work-orders/work-orders-api';

import { AssignDialog, type AssignSelection } from '@work-orders/schedule-actions/assign-dialog/assign-dialog';
import type { AssignFormValue } from '@work-orders/schedule-actions/assign-dialog/assign-dialog-schema';
import { ScheduleActionsStore } from '@work-orders/schedule-actions/schedule-actions.store';

@Component({
  imports: [ActionDialog, AssignDialog, FormsModule, LoadingSpinner, Select, Tag, TranslatePipe],
  host: { class: 'contents' },
  providers: [WorkOrdersApi, ScheduleActionsStore],
  selector: 'hf-schedule-actions-host',
  templateUrl: './schedule-actions-host.html',
})
export class ScheduleActionsHost {
  readonly action = input.required<AppAction>();

  protected readonly store = inject(ScheduleActionsStore);

  protected readonly title = computed(() => {
    switch (this.store.type()) {
      case WorkOrderActionType.Move:
        return 'WORK_ORDERS.SCHEDULE.MOVE_TITLE';
      case WorkOrderActionType.ChangeStatus:
        return 'WORK_ORDERS.SCHEDULE.STATUS_TITLE';
      default:
        return 'WORK_ORDERS.SCHEDULE.ASSIGN_TITLE';
    }
  });
  protected readonly statusChoice = signal<WorkOrderStatus | null>(null);

  private readonly _current = signal<AssignFormValue | null>(null);
  private readonly _isValid = signal(false);

  protected readonly saveDisabledReason = computed((): string | null => {
    if (this.store.isStatusDialog()) {
      return this.statusChoice() === null ? 'WORK_ORDERS.SCHEDULE.PICK_STATUS' : null;
    }
    return this._isValid() ? null : 'WORK_ORDERS.FORM.INVALID';
  });
  protected readonly statusOptionList = computed(() =>
    this.store.statusOptions().map((value) => ({ value, labelKey: STATUS_LABEL_KEYS[value] })),
  );

  constructor() {
    effect(() => {
      this.store.start(this.action());
    });
    effect(() => {
      if (this.store.workOrder.hasValue()) {
        untracked(() => this.store.requestConfirmation());
      }
    });
  }

  protected onSelection(selection: AssignSelection | null): void {
    this.store.select(selection);
  }

  protected onSave(): void {
    if (this.saveDisabledReason() !== null) {
      return;
    }
    const status = this.statusChoice();
    const value = this._current();
    if (this.store.isStatusDialog() && status !== null) {
      this.store.submitStatus(status);
    } else if (value !== null) {
      this.store.submitSchedule(value);
    }
  }

  protected onValue(value: AssignFormValue): void {
    this._current.set(value);
  }

  protected onValidity(isValid: boolean): void {
    this._isValid.set(isValid);
  }
}
