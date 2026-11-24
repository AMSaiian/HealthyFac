import { Component, computed, effect, input, linkedSignal, output, signal } from '@angular/core';
import { type FieldTree, form, FormField, readonly, validateStandardSchema } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';

import { isFieldInvalid } from '@shared/forms/field-state';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { keepSelectedOptions, type LookupOption, type LookupRequest } from '@shared/lookups/lookup-options';
import { RichTextEditor } from '@shared/rich-text/rich-text-editor/rich-text-editor';
import type { Temporal } from '@shared/time/temporal';

import {
  AssignmentPicker,
  type DaysRequest,
  type SpansRequest,
} from '@work-orders/assignment-picker/assignment-picker';
import type { AssignmentValue } from '@work-orders/assignment-picker/assignment-picker.utils';
import { type FreeSpan, type ScheduleDay, WorkOrderCategory, WorkOrderPriority } from '@work-orders/work-order';
import { WorkOrderFiles } from '@work-orders/drawer/work-order-files/work-order-files';
import type { FileEntry } from '@work-orders/drawer/work-order-drawer-store/work-order-drawer.store.utils';
import { CATEGORY_LABEL_KEYS, PRIORITY_LABEL_KEYS } from '@work-orders/list/work-order-grid/work-order-grid.utils';
import {
  DESCRIPTION_MAX_LENGTH,
  ESTIMATE_MAX_MINUTES,
  ESTIMATE_MIN_MINUTES,
  ESTIMATE_STEP_MINUTES,
  ESTIMATE_SUFFIX,
  FILE_MAX_COUNT,
} from '@work-orders/work-order-limits';
import { workOrderFormSchema, type WorkOrderFormValue } from '@work-orders/work-order-schema';
import { Divider } from 'primeng/divider';

@Component({
  imports: [
    AssignmentPicker,
    Divider,
    FormField,
    InputNumber,
    InputText,
    RichTextEditor,
    Select,
    TranslatePipe,
    WorkOrderFiles,
  ],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-work-order-form',
  templateUrl: './work-order-form.html',
})
export class WorkOrderForm {
  readonly initialValue = input.required<WorkOrderFormValue>();
  readonly machines = input<LookupOption[]>([]);
  readonly isMachinesLoading = input(false);
  protected readonly showMachinesLoading = delayedLoading(this.isMachinesLoading);
  readonly fileEntries = input<readonly FileEntry[]>([]);
  readonly isFilesLoading = input(false);
  readonly isEstimateReadOnly = input(false);
  readonly isScheduleShown = input(false);
  readonly isScheduleReadOnly = input(false);
  readonly employees = input<LookupOption[]>([]);
  readonly isEmployeesLoading = input(false);
  readonly days = input<readonly ScheduleDay[] | null>(null);
  readonly isDaysLoading = input(false);
  readonly spans = input<readonly FreeSpan[] | null>(null);
  readonly isSpansLoading = input(false);
  readonly today = input<Temporal.PlainDate | null>(null);

  readonly valueChange = output<WorkOrderFormValue>();
  readonly validityChange = output<boolean>();
  readonly uploadChosen = output<File>();
  readonly pickRequested = output<void>();
  readonly machineLookupRequested = output<LookupRequest>();
  readonly employeeLookupRequested = output<LookupRequest>();
  readonly daysRequested = output<DaysRequest>();
  readonly spansRequested = output<SpansRequest>();

  protected readonly descriptionMaxLength = DESCRIPTION_MAX_LENGTH;
  protected readonly estimateMin = ESTIMATE_MIN_MINUTES;
  protected readonly estimateMax = ESTIMATE_MAX_MINUTES;
  protected readonly estimateStep = ESTIMATE_STEP_MINUTES;
  protected readonly estimateSuffix = ESTIMATE_SUFFIX;
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
    readonly(path.estimateMinutes, { when: () => this.isEstimateReadOnly() });
  });

  protected readonly machineList = keepSelectedOptions(this.machines, () => [this.model().machineId]);
  protected readonly pickerInitial = computed((): AssignmentValue => ({
    assigneeId: this.initialValue().assigneeId,
    start: this.initialValue().start,
  }));

  private readonly _isScheduleValid = signal(true);

  constructor() {
    effect(() => {
      this.valueChange.emit(this.model());
    });
    effect(() => {
      this.validityChange.emit(this.workOrderForm().valid() && this._isScheduleValid());
    });
  }

  addFileIds(ids: readonly string[]): void {
    this.model.update((value) => ({
      ...value,
      fileIds: [...value.fileIds, ...ids.filter((id) => !value.fileIds.includes(id))],
    }));
  }

  protected onScheduleChange(value: AssignmentValue): void {
    this.model.update((current) => ({ ...current, assigneeId: value.assigneeId, start: value.start }));
  }

  protected onScheduleValidity(isValid: boolean): void {
    this._isScheduleValid.set(isValid);
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
