import { Component, computed, input, model, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import type { FormValueControl } from '@angular/forms/signals';
import { DatePicker } from 'primeng/datepicker';

import { isSameDate, pickerDateToPlainDate, plainDateToPickerDate } from '@shared/time/pickers/picker-date-conversion';
import type { Temporal } from '@shared/time/temporal';

@Component({
  imports: [DatePicker, FormsModule],
  host: { class: 'block' },
  selector: 'hf-plain-date-picker',
  templateUrl: './plain-date-picker.html',
})
export class PlainDatePicker implements FormValueControl<Temporal.PlainDate | null> {
  readonly value = model<Temporal.PlainDate | null>(null);
  readonly disabled = input(false);
  readonly readonly = input(false);
  readonly invalid = input(false);
  readonly appendTo = input<'body' | 'self'>('body');
  readonly min = input<Temporal.PlainDate | undefined>(undefined);
  readonly max = input<Temporal.PlainDate | undefined>(undefined);
  readonly touch = output<void>();

  protected readonly pickerValue = computed(() => plainDateToPickerDate(this.value()), { equal: isSameDate });
  protected readonly minDate = computed(() => plainDateToPickerDate(this.min() ?? null), { equal: isSameDate });
  protected readonly maxDate = computed(() => plainDateToPickerDate(this.max() ?? null), { equal: isSameDate });

  protected onPickerChange(date: Date | Date[] | null | undefined): void {
    const next = pickerDateToPlainDate(date instanceof Date ? date : null);
    if (!isSameDate(plainDateToPickerDate(next), this.pickerValue())) {
      this.value.set(next);
    }
  }
}
