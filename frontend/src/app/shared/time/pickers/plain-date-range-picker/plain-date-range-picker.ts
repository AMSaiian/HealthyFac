import { Component, computed, input, model, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import type { FormValueControl } from '@angular/forms/signals';
import { DatePicker } from 'primeng/datepicker';

import {
  areSameDates,
  pickerDatesToPlainDateRange,
  plainDateRangeToPickerDates,
} from '@shared/time/pickers/picker-date-conversion';
import type { PlainDateRange } from '@shared/time/plain-date-range';

@Component({
  imports: [DatePicker, FormsModule],
  host: { class: 'block' },
  selector: 'hf-plain-date-range-picker',
  templateUrl: './plain-date-range-picker.html',
})
export class PlainDateRangePicker implements FormValueControl<PlainDateRange> {
  readonly value = model<PlainDateRange>({ from: null, to: null });
  readonly disabled = input(false);
  readonly readonly = input(false);
  readonly invalid = input(false);
  readonly appendTo = input<'body' | 'self'>('body');
  readonly touch = output<void>();

  protected readonly pickerValue = computed(() => plainDateRangeToPickerDates(this.value()), { equal: areSameDates });

  protected onPickerChange(dates: Date | Date[] | null | undefined): void {
    const next = pickerDatesToPlainDateRange(Array.isArray(dates) ? dates : null);
    if (!areSameDates(plainDateRangeToPickerDates(next), this.pickerValue())) {
      this.value.set(next);
    }
  }
}
