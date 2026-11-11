import { Component, computed, input, model, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import type { FormValueControl } from '@angular/forms/signals';
import { DatePicker } from 'primeng/datepicker';

import {
  isSameDate,
  pickerDateToPlainDateTime,
  plainDateTimeToPickerDate,
} from '@shared/time/pickers/picker-date-conversion';
import type { Temporal } from '@shared/time/temporal';

@Component({
  imports: [DatePicker, FormsModule],
  host: { class: 'block' },
  selector: 'hf-plain-date-time-picker',
  templateUrl: './plain-date-time-picker.html',
})
export class PlainDateTimePicker implements FormValueControl<Temporal.PlainDateTime | null> {
  readonly value = model<Temporal.PlainDateTime | null>(null);
  readonly disabled = input(false);
  readonly readonly = input(false);
  readonly invalid = input(false);
  readonly appendTo = input<'body' | 'self'>('body');
  readonly min = input<Temporal.PlainDateTime | undefined>(undefined);
  readonly max = input<Temporal.PlainDateTime | undefined>(undefined);
  readonly stepMinute = input(1);
  readonly touch = output<void>();

  protected readonly pickerValue = computed(() => plainDateTimeToPickerDate(this.value()), { equal: isSameDate });
  protected readonly minDate = computed(() => plainDateTimeToPickerDate(this.min() ?? null), { equal: isSameDate });
  protected readonly maxDate = computed(() => plainDateTimeToPickerDate(this.max() ?? null), { equal: isSameDate });

  protected onPickerChange(date: Date | Date[] | null | undefined): void {
    const next = pickerDateToPlainDateTime(date instanceof Date ? date : null);
    if (!isSameDate(plainDateTimeToPickerDate(next), this.pickerValue())) {
      this.value.set(next);
    }
  }
}
