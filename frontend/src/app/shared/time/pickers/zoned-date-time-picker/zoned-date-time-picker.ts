import { Component, computed, input, model, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import type { FormValueControl } from '@angular/forms/signals';
import { DatePicker } from 'primeng/datepicker';

import { pickerDateToZonedDateTime, zonedDateTimeToPickerDate } from '@shared/time/pickers/picker-date-conversion';
import type { Temporal } from '@shared/time/temporal';

@Component({
  imports: [DatePicker, FormsModule],
  host: { class: 'block' },
  selector: 'hf-zoned-date-time-picker',
  templateUrl: './zoned-date-time-picker.html',
})
export class ZonedDateTimePicker implements FormValueControl<Temporal.ZonedDateTime | null> {
  readonly value = model<Temporal.ZonedDateTime | null>(null);
  readonly timeZone = input.required<string>();
  readonly disabled = input(false);
  readonly readonly = input(false);
  readonly invalid = input(false);
  readonly min = input<Temporal.ZonedDateTime | undefined>(undefined);
  readonly max = input<Temporal.ZonedDateTime | undefined>(undefined);
  readonly touch = output<void>();

  protected readonly pickerValue = computed(() => zonedDateTimeToPickerDate(this.value()));
  protected readonly minDate = computed(() => zonedDateTimeToPickerDate(this.min() ?? null));
  protected readonly maxDate = computed(() => zonedDateTimeToPickerDate(this.max() ?? null));

  protected onPickerChange(date: Date | Date[] | null | undefined): void {
    this.value.set(pickerDateToZonedDateTime(date instanceof Date ? date : null, this.timeZone()));
  }
}
