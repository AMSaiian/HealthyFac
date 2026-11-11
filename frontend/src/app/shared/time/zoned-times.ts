import type { Temporal } from './temporal';

const DISAMBIGUATION = 'compatible';

/** The half-open range of one local date: `start` belongs to the date, `end` is the start of the next date. */
export interface DayRange {
  readonly start: Temporal.Instant;
  readonly end: Temporal.Instant;
}

/**
 * Converts a local date and time of a zone to an instant with the rule `compatible`, like the backend `LocalTimes`:
 * a skipped time (spring gap) moves forward by the gap, and a repeated time (autumn overlap) takes the earlier instant.
 */
export function toInstant(date: Temporal.PlainDate, time: Temporal.PlainTime, timeZoneId: string): Temporal.Instant {
  return date.toPlainDateTime(time).toZonedDateTime(timeZoneId, { disambiguation: DISAMBIGUATION }).toInstant();
}

/** Tells whether the local time does not exist in the zone (it falls in the spring gap). */
export function isSkipped(date: Temporal.PlainDate, time: Temporal.PlainTime, timeZoneId: string): boolean {
  const dateTime = date.toPlainDateTime(time);
  const resolved = dateTime.toZonedDateTime(timeZoneId, { disambiguation: DISAMBIGUATION }).toPlainDateTime();

  return !resolved.equals(dateTime);
}

/** Gives the instants from the start of the date to the start of the next date in the zone (23, 24, or 25 hours). */
export function dayRange(date: Temporal.PlainDate, timeZoneId: string): DayRange {
  return {
    start: date.toZonedDateTime(timeZoneId).toInstant(),
    end: date.add({ days: 1 }).toZonedDateTime(timeZoneId).toInstant(),
  };
}
