import { Component, computed, effect, input, linkedSignal, output } from '@angular/core';
import { form, validateStandardSchema } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';

import { PlainDatePicker } from '@shared/time/pickers/plain-date-picker/plain-date-picker';
import type { Temporal } from '@shared/time/temporal';

import { BlockListEditor } from '@employees/shifts/block-list-editor/block-list-editor';
import type { ShiftBlock, Weekday } from '@employees/shifts/shift';
import { WEEKDAY_KEYS } from '@employees/shifts/shifts-store/shifts.store.utils';
import { versionFormSchema, type VersionFormValue } from '@employees/shifts/version-dialog/version-dialog-schema';

/** The form of a version: the first day of effect (a new version only) and the blocks of the 7 weekdays. */
@Component({
  imports: [BlockListEditor, PlainDatePicker, TranslatePipe],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-version-dialog',
  templateUrl: './version-dialog.html',
})
export class VersionDialog {
  readonly initialValue = input.required<VersionFormValue>();
  readonly isEdit = input(false);
  readonly minDate = input.required<Temporal.PlainDate>();

  readonly valueChange = output<VersionFormValue>();
  readonly validityChange = output<boolean>();

  protected readonly model = linkedSignal<VersionFormValue>(() => ({
    effectiveFrom: this.initialValue().effectiveFrom,
    days: this.initialValue().days.map((day) => ({ weekday: day.weekday, blocks: [...day.blocks] })),
  }));
  protected readonly versionForm = form(this.model, (path) => {
    validateStandardSchema(path, versionFormSchema);
  });
  protected readonly messages = computed(() => [
    ...new Set(
      this.versionForm()
        .errorSummary()
        .map((error) => error.message ?? ''),
    ),
  ]);

  constructor() {
    effect(() => {
      this.valueChange.emit(this.model());
    });
    effect(() => {
      this.validityChange.emit(this.versionForm().valid());
    });
  }

  protected setDate(date: Temporal.PlainDate | null): void {
    this.model.update((value) => ({ ...value, effectiveFrom: date }));
  }

  protected setBlocks(weekday: Weekday, blocks: readonly ShiftBlock[]): void {
    this.model.update((value) => ({
      ...value,
      days: value.days.map((day) => (day.weekday === weekday ? { ...day, blocks: [...blocks] } : day)),
    }));
  }

  protected weekdayKey(weekday: Weekday): string {
    return WEEKDAY_KEYS[weekday];
  }
}
