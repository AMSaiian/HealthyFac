import type { PlainDateRange } from '@shared/time/plain-date-range';
import { Temporal } from '@shared/time/temporal';

const MONTH_OFFSET = 1;

export function plainDateToPickerDate(value: Temporal.PlainDate | null): Date | null {
  return value === null ? null : new Date(value.year, value.month - MONTH_OFFSET, value.day);
}

export function pickerDateToPlainDate(value: Date | null): Temporal.PlainDate | null {
  return value === null
    ? null
    : new Temporal.PlainDate(value.getFullYear(), value.getMonth() + MONTH_OFFSET, value.getDate());
}

/** Gives the value of a range date picker: the two days, or `null` when the range is empty. */
export function plainDateRangeToPickerDates(value: PlainDateRange): (Date | null)[] | null {
  return value.from === null && value.to === null
    ? null
    : [plainDateToPickerDate(value.from), plainDateToPickerDate(value.to)];
}

/** Reads the value of a range date picker. A range with one day has an open end. */
export function pickerDatesToPlainDateRange(value: readonly (Date | null)[] | null | undefined): PlainDateRange {
  return {
    from: pickerDateToPlainDate(value?.[0] ?? null),
    to: pickerDateToPlainDate(value?.[1] ?? null),
  };
}

export function plainDateTimeToPickerDate(value: Temporal.PlainDateTime | null): Date | null {
  return value === null ? null : new Date(value.year, value.month - MONTH_OFFSET, value.day, value.hour, value.minute);
}

export function pickerDateToPlainDateTime(value: Date | null): Temporal.PlainDateTime | null {
  return value === null
    ? null
    : new Temporal.PlainDateTime(
        value.getFullYear(),
        value.getMonth() + MONTH_OFFSET,
        value.getDate(),
        value.getHours(),
        value.getMinutes(),
      );
}

export function zonedDateTimeToPickerDate(value: Temporal.ZonedDateTime | null): Date | null {
  return value === null ? null : plainDateTimeToPickerDate(value.toPlainDateTime());
}

export function pickerDateToZonedDateTime(value: Date | null, timeZone: string): Temporal.ZonedDateTime | null {
  const plain = pickerDateToPlainDateTime(value);
  return plain === null ? null : plain.toZonedDateTime(timeZone, { disambiguation: 'compatible' });
}
