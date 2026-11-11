import { Component, effect, input, linkedSignal, output } from '@angular/core';
import { type FieldTree, form, FormField, validateStandardSchema } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';

import { isFieldInvalid } from '@shared/forms/field-state';
import { keepSelectedOptions, type LookupOption, type LookupRequest } from '@shared/lookups/lookup-options';
import { RichTextEditor } from '@shared/rich-text/rich-text-editor/rich-text-editor';

import { WorkOrderCategory, WorkOrderPriority } from '@work-orders/work-order';
import { WorkOrderFiles } from '@work-orders/drawer/work-order-files/work-order-files';
import type { FileEntry } from '@work-orders/drawer/work-order-drawer.store.utils';
import { CATEGORY_LABEL_KEYS, PRIORITY_LABEL_KEYS } from '@work-orders/list/work-order-grid/work-order-grid.utils';
import {
  DESCRIPTION_MAX_LENGTH,
  ESTIMATE_MAX_MINUTES,
  ESTIMATE_MIN_MINUTES,
  ESTIMATE_STEP_MINUTES,
  FILE_MAX_COUNT,
} from '@work-orders/work-order-limits';
import { workOrderFormSchema, type WorkOrderFormValue } from '@work-orders/work-order-schema';

@Component({
  imports: [FormField, InputNumber, InputText, RichTextEditor, Select, TranslatePipe, WorkOrderFiles],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-work-order-form',
  templateUrl: './work-order-form.html',
})
export class WorkOrderForm {
  readonly initialValue = input.required<WorkOrderFormValue>();
  readonly machines = input<LookupOption[]>([]);
  readonly isMachinesLoading = input(false);
  readonly fileEntries = input<readonly FileEntry[]>([]);
  readonly isFilesLoading = input(false);
  readonly isEstimateReadOnly = input(false);

  readonly valueChange = output<WorkOrderFormValue>();
  readonly validityChange = output<boolean>();
  readonly uploadChosen = output<File>();
  readonly pickRequested = output<void>();
  readonly machineLookupRequested = output<LookupRequest>();

  protected readonly descriptionMaxLength = DESCRIPTION_MAX_LENGTH;
  protected readonly estimateMin = ESTIMATE_MIN_MINUTES;
  protected readonly estimateMax = ESTIMATE_MAX_MINUTES;
  protected readonly estimateStep = ESTIMATE_STEP_MINUTES;
  protected readonly priorityOptions = Object.values(WorkOrderPriority).map((value) => ({
    value,
    labelKey: PRIORITY_LABEL_KEYS[value],
  }));
  protected readonly categoryOptions = Object.values(WorkOrderCategory).map((value) => ({
    value,
    labelKey: CATEGORY_LABEL_KEYS[value],
  }));

  protected readonly model = linkedSignal<WorkOrderFormValue>(() => ({ ...this.initialValue() }));
  protected readonly workOrderForm = form(this.model, (path) => {
    validateStandardSchema(path, workOrderFormSchema);
  });

  protected readonly machineList = keepSelectedOptions(this.machines, () => [this.model().machineId]);

  constructor() {
    effect(() => {
      this.valueChange.emit(this.model());
    });
    effect(() => {
      this.validityChange.emit(this.workOrderForm().valid());
    });
  }

  addFileIds(ids: readonly string[]): void {
    this.model.update((value) => ({
      ...value,
      fileIds: [...value.fileIds, ...ids.filter((id) => !value.fileIds.includes(id))],
    }));
  }

  protected removeFile(id: string): void {
    this.model.update((value) => ({ ...value, fileIds: value.fileIds.filter((candidate) => candidate !== id) }));
  }

  protected isFull(): boolean {
    return this.model().fileIds.length >= FILE_MAX_COUNT;
  }

  protected isShown(field: FieldTree<unknown>): boolean {
    return isFieldInvalid({ isInvalid: field().invalid(), isTouched: field().touched() });
  }
}
