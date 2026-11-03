import { Component, computed, input, model, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import type { FormValueControl } from '@angular/forms/signals';
import { DatePicker } from 'primeng/datepicker';

import { pickerDateToPlainDateTime, plainDateTimeToPickerDate } from '@shared/time/picker-date-conversion';
import type { Temporal } from '@shared/time/temporal';

@Component({
  imports: [DatePicker, FormsModule],
  selector: 'hf-plain-date-time-picker',
  templateUrl: './plain-date-time-picker.html',
})
export class PlainDateTimePicker implements FormValueControl<Temporal.PlainDateTime | null> {
  readonly value = model<Temporal.PlainDateTime | null>(null);
  readonly disabled = input(false);
  readonly readonly = input(false);
  readonly invalid = input(false);
  readonly min = input<Temporal.PlainDateTime | undefined>(undefined);
  readonly max = input<Temporal.PlainDateTime | undefined>(undefined);
  readonly touch = output<void>();

  protected readonly pickerValue = computed(() => plainDateTimeToPickerDate(this.value()));
  protected readonly minDate = computed(() => plainDateTimeToPickerDate(this.min() ?? null));
  protected readonly maxDate = computed(() => plainDateTimeToPickerDate(this.max() ?? null));

  protected onPickerChange(date: Date | Date[] | null | undefined): void {
    this.value.set(pickerDateToPlainDateTime(date instanceof Date ? date : null));
  }
}
