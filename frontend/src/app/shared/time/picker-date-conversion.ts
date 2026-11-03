import { Temporal } from './temporal';

const MONTH_OFFSET = 1;

export function plainDateToPickerDate(value: Temporal.PlainDate | null): Date | null {
  return value === null ? null : new Date(value.year, value.month - MONTH_OFFSET, value.day);
}

export function pickerDateToPlainDate(value: Date | null): Temporal.PlainDate | null {
  return value === null
    ? null
    : new Temporal.PlainDate(value.getFullYear(), value.getMonth() + MONTH_OFFSET, value.getDate());
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
