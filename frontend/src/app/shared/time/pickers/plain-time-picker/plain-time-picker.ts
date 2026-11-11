import { Component, computed, input, model, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import type { FormValueControl } from '@angular/forms/signals';
import { DatePicker } from 'primeng/datepicker';

import { isSameDate, pickerDateToPlainTime, plainTimeToPickerDate } from '@shared/time/pickers/picker-date-conversion';
import type { Temporal } from '@shared/time/temporal';

const DEFAULT_STEP_MINUTE = 5;

/** A 24-hour time picker for a `Temporal.PlainTime`. The panel opens on `body`, so a dialog never clips it. */
@Component({
  imports: [DatePicker, FormsModule],
  host: { class: 'block' },
  selector: 'hf-plain-time-picker',
  templateUrl: './plain-time-picker.html',
})
export class PlainTimePicker implements FormValueControl<Temporal.PlainTime | null> {
  readonly value = model<Temporal.PlainTime | null>(null);
  readonly disabled = input(false);
  readonly readonly = input(false);
  readonly invalid = input(false);
  readonly stepMinute = input(DEFAULT_STEP_MINUTE);
  readonly max = input<Temporal.PlainTime | undefined>(undefined);
  readonly appendTo = input<'body' | 'self'>('body');
  readonly touch = output<void>();

  protected readonly pickerValue = computed(() => plainTimeToPickerDate(this.value()), { equal: isSameDate });
  protected readonly minDate = computed(() => undefined);
  protected readonly maxDate = computed(() => plainTimeToPickerDate(this.max() ?? null) ?? undefined, {
    equal: isSameDate,
  });

  protected onPickerChange(date: Date | Date[] | null | undefined): void {
    const next = pickerDateToPlainTime(date instanceof Date ? date : null);
    if (!isSameDate(plainTimeToPickerDate(next), this.pickerValue())) {
      this.value.set(next);
    }
  }
}
