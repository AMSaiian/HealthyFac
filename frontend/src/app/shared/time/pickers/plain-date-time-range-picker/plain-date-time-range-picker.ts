import { Component, computed, input, model, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import type { FormValueControl } from '@angular/forms/signals';
import { DatePicker } from 'primeng/datepicker';

import {
  areSameDates,
  pickerDatesToPlainDateTimeRange,
  plainDateTimeRangeToPickerDates,
} from '@shared/time/pickers/picker-date-conversion';
import { EMPTY_DATE_TIME_RANGE, type PlainDateTimeRange } from '@shared/time/plain-date-time-range';

/** A range picker with times. The value holds local moments; the caller converts them with its zone. */
@Component({
  imports: [DatePicker, FormsModule],
  host: { class: 'block' },
  selector: 'hf-plain-date-time-range-picker',
  templateUrl: './plain-date-time-range-picker.html',
})
export class PlainDateTimeRangePicker implements FormValueControl<PlainDateTimeRange> {
  readonly value = model<PlainDateTimeRange>(EMPTY_DATE_TIME_RANGE);
  readonly disabled = input(false);
  readonly readonly = input(false);
  readonly invalid = input(false);
  readonly appendTo = input<'body' | 'self'>('body');
  readonly placeholder = input('');
  readonly touch = output<void>();

  protected readonly pickerValue = computed(() => plainDateTimeRangeToPickerDates(this.value()), {
    equal: areSameDates,
  });

  protected onPickerChange(dates: Date | Date[] | null | undefined): void {
    const next = pickerDatesToPlainDateTimeRange(Array.isArray(dates) ? dates : null);
    if (!areSameDates(plainDateTimeRangeToPickerDates(next), this.pickerValue())) {
      this.value.set(next);
    }
  }
}
