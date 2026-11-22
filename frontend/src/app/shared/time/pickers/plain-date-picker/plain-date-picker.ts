import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, input, model, output, type TemplateRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import type { FormValueControl } from '@angular/forms/signals';
import { DatePicker, type DatePickerMonthChangeEvent } from 'primeng/datepicker';

import { isSameDate, pickerDateToPlainDate, plainDateToPickerDate } from '@shared/time/pickers/picker-date-conversion';
import type { Temporal } from '@shared/time/temporal';

/** A calendar cell as PrimeNG gives it to a cell template: the month is 0-based. */
export interface PickerCellContext {
  readonly year: number;
  readonly month: number;
  readonly day: number;
}

/** The month that the panel shows after a month or year change: the month is 1-based. */
export interface ShownMonth {
  readonly year: number;
  readonly month: number;
}

@Component({
  imports: [DatePicker, FormsModule, NgTemplateOutlet],
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
  /** A template for the day cells. It gets the cell as `$implicit`. */
  readonly dateTemplate = input<TemplateRef<{ $implicit: PickerCellContext }> | null>(null);
  readonly touch = output<void>();
  readonly monthChange = output<ShownMonth>();
  readonly shown = output<void>();

  protected readonly pickerValue = computed(() => plainDateToPickerDate(this.value()), { equal: isSameDate });
  protected readonly minDate = computed(() => plainDateToPickerDate(this.min() ?? null), { equal: isSameDate });
  protected readonly maxDate = computed(() => plainDateToPickerDate(this.max() ?? null), { equal: isSameDate });

  protected onViewChange(event: DatePickerMonthChangeEvent): void {
    if (event.year !== undefined && event.month !== undefined) {
      this.monthChange.emit({ year: event.year, month: event.month });
    }
  }

  protected onPickerChange(date: Date | Date[] | null | undefined): void {
    const next = pickerDateToPlainDate(date instanceof Date ? date : null);
    if (!isSameDate(plainDateToPickerDate(next), this.pickerValue())) {
      this.value.set(next);
    }
  }
}
