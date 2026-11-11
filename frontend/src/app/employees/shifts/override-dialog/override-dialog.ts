import { Component, computed, effect, input, linkedSignal, output } from '@angular/core';
import { form, validateStandardSchema } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';

import { PlainDatePicker } from '@shared/time/pickers/plain-date-picker/plain-date-picker';
import type { Temporal } from '@shared/time/temporal';

import { BlockListEditor } from '@employees/shifts/block-list-editor/block-list-editor';
import { overrideFormSchema, type OverrideFormValue } from '@employees/shifts/override-dialog/override-dialog-schema';
import type { ShiftBlock } from '@employees/shifts/shift';

/** The form of an override: one date and its blocks. No block means a day off. */
@Component({
  imports: [BlockListEditor, PlainDatePicker, TranslatePipe],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-override-dialog',
  templateUrl: './override-dialog.html',
})
export class OverrideDialog {
  readonly initialValue = input.required<OverrideFormValue>();
  readonly isEdit = input(false);
  readonly minDate = input.required<Temporal.PlainDate>();

  readonly valueChange = output<OverrideFormValue>();
  readonly validityChange = output<boolean>();

  protected readonly model = linkedSignal<OverrideFormValue>(() => ({
    date: this.initialValue().date,
    blocks: [...this.initialValue().blocks],
  }));
  protected readonly overrideForm = form(this.model, (path) => {
    validateStandardSchema(path, overrideFormSchema);
  });
  protected readonly messages = computed(() => [
    ...new Set(
      this.overrideForm()
        .errorSummary()
        .map((error) => error.message ?? ''),
    ),
  ]);

  constructor() {
    effect(() => {
      this.valueChange.emit(this.model());
    });
    effect(() => {
      this.validityChange.emit(this.overrideForm().valid());
    });
  }

  protected setDate(date: Temporal.PlainDate | null): void {
    this.model.update((value) => ({ ...value, date }));
  }

  protected setBlocks(blocks: readonly ShiftBlock[]): void {
    this.model.update((value) => ({ ...value, blocks: [...blocks] }));
  }
}
