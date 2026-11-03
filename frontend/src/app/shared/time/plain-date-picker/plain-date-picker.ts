import { Component, computed, input, model, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import type { FormValueControl } from '@angular/forms/signals';
import { DatePicker } from 'primeng/datepicker';

import { pickerDateToPlainDate, plainDateToPickerDate } from '@shared/time/picker-date-conversion';
import type { Temporal } from '@shared/time/temporal';

@Component({
  imports: [DatePicker, FormsModule],
  selector: 'hf-plain-date-picker',
  templateUrl: './plain-date-picker.html',
})
export class PlainDatePicker implements FormValueControl<Temporal.PlainDate | null> {
  readonly value = model<Temporal.PlainDate | null>(null);
  readonly disabled = input(false);
  readonly readonly = input(false);
  readonly invalid = input(false);
  readonly min = input<Temporal.PlainDate | undefined>(undefined);
  readonly max = input<Temporal.PlainDate | undefined>(undefined);
  readonly touch = output<void>();

  protected readonly pickerValue = computed(() => plainDateToPickerDate(this.value()));
  protected readonly minDate = computed(() => plainDateToPickerDate(this.min() ?? null));
  protected readonly maxDate = computed(() => plainDateToPickerDate(this.max() ?? null));

  protected onPickerChange(date: Date | Date[] | null | undefined): void {
    this.value.set(pickerDateToPlainDate(date instanceof Date ? date : null));
  }
}
