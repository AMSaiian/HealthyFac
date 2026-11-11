import { Component, computed, effect, inject, input, signal, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tab, TabList, TabPanel, TabPanels, Tabs } from 'primeng/tabs';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import type { AppAction } from '@shared/actions/app-action';
import { hasFormChanges } from '@shared/forms/form-changes';
import { LoadingSpinner } from '@shared/loading/loading-spinner/loading-spinner';

import { FilesApi } from '@files/files-api';
import { WorkOrderDrawerStore } from '@work-orders/drawer/work-order-drawer.store';
import { FilePickerDialog } from '@work-orders/drawer/file-picker-dialog/file-picker-dialog';
import { STATUS_LABEL_KEYS } from '@work-orders/list/work-order-grid/work-order-grid.utils';
import { WorkOrderForm } from '@work-orders/drawer/work-order-form/work-order-form';
import { WorkOrderHistory } from '@work-orders/drawer/work-order-history/work-order-history';
import { workOrderFormSchema, type WorkOrderFormValue } from '@work-orders/work-order-schema';
import { WorkOrdersApi } from '@work-orders/work-orders-api';

const DETAILS_TAB = 'details';
const HISTORY_TAB = 'history';

@Component({
  imports: [
    ActionDialog,
    ButtonDirective,
    FilePickerDialog,
    LoadingSpinner,
    Tab,
    TabList,
    TabPanel,
    TabPanels,
    Tabs,
    Tag,
    Tooltip,
    TranslatePipe,
    WorkOrderForm,
    WorkOrderHistory,
  ],
  host: { class: 'contents' },
  providers: [WorkOrdersApi, FilesApi, WorkOrderDrawerStore],
  selector: 'hf-work-order-drawer-host',
  templateUrl: './work-order-drawer-host.html',
})
export class WorkOrderDrawerHost {
  readonly action = input.required<AppAction>();

  protected readonly store = inject(WorkOrderDrawerStore);
  protected readonly DETAILS_TAB = DETAILS_TAB;
  protected readonly HISTORY_TAB = HISTORY_TAB;
  protected readonly tab = signal(DETAILS_TAB);
  protected readonly statusLabelKeys = STATUS_LABEL_KEYS;

  private readonly _form = viewChild(WorkOrderForm);
  private readonly _current = signal<WorkOrderFormValue | null>(null);
  private readonly _isValid = signal(false);

  protected readonly hasNoChanges = computed(() => {
    const initial = this.store.initialValue();
    const current = this._current();
    return (
      this.store.isUpdate() &&
      initial !== null &&
      current !== null &&
      !hasFormChanges(workOrderFormSchema, initial, current)
    );
  });
  protected readonly saveDisabledReason = computed((): string | null => {
    if (!this._isValid()) {
      return 'WORK_ORDERS.FORM.INVALID';
    }
    return this.hasNoChanges() ? 'WORK_ORDERS.FORM.NO_CHANGES' : null;
  });
  protected readonly isEstimateReadOnly = computed(() => this.store.isUpdate() && !this.store.canManage());
  protected readonly history = computed(() => (this.store.history.hasValue() ? this.store.history.value() : []));

  constructor() {
    effect(() => {
      this.store.start(this.action());
    });
    effect(() => {
      if (this.tab() === HISTORY_TAB) {
        this.store.activateHistory();
      }
    });
  }

  protected onValueChange(value: WorkOrderFormValue): void {
    this._current.set(value);
    this.store.setFormState(value.fileIds, value.machineId);
  }

  protected onValidityChange(isValid: boolean): void {
    this._isValid.set(isValid);
  }

  protected onUpload(file: File): void {
    this.store.upload(file).subscribe((id) => this._form()?.addFileIds([id]));
  }

  protected onPicked(ids: readonly string[]): void {
    this._form()?.addFileIds(ids);
    this.store.closePicker();
  }

  protected onSave(): void {
    const value = this._current();
    if (value !== null && this.saveDisabledReason() === null) {
      this.store.save(value);
    }
  }
}
