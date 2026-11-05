import type { PlainDateRange } from '@shared/time/plain-date-range';
import type { Temporal } from '@shared/time/temporal';

/** A backend `TextFilter`: starts with, case-insensitive. */
export interface TextFilterBody {
  readonly value: string;
}

/** A backend `IdSetFilter`. The property name `ids` makes the backend decrypt the IDs. */
export interface IdSetBody {
  readonly ids: readonly string[];
}

/** A backend `SetFilter<T>`. */
export interface SetFilterBody<TValue> {
  readonly values: readonly TValue[];
}

/** A backend `DateRangeFilter`: UTC instants, `from` inclusive, `to` exclusive. */
export interface DateRangeBody {
  readonly from?: string;
  readonly to?: string;
}

export function textFilter(value: string): TextFilterBody | undefined {
  return value === '' ? undefined : { value };
}

export function idSetFilter(ids: readonly string[]): IdSetBody | undefined {
  return ids.length === 0 ? undefined : { ids };
}

export function setFilter<TValue>(values: readonly TValue[]): SetFilterBody<TValue> | undefined {
  return values.length === 0 ? undefined : { values };
}

/**
 * The picked days are inclusive in the UI. On the wire the range is half-open: `from` is the start of the first day,
 * and `to` is the start of the day after the last day, both in `timeZoneId` and converted to UTC.
 */
export function dateRangeFilter(range: PlainDateRange, timeZoneId: string): DateRangeBody | undefined {
  if (range.from === null && range.to === null) {
    return undefined;
  }
  return {
    ...(range.from === null ? {} : { from: startOfDayUtc(range.from, timeZoneId) }),
    ...(range.to === null ? {} : { to: startOfDayUtc(range.to.add({ days: 1 }), timeZoneId) }),
  };
}

function startOfDayUtc(day: Temporal.PlainDate, timeZoneId: string): string {
  return day.toZonedDateTime(timeZoneId).toInstant().toString();
}
