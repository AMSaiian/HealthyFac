import {
  pickerDateToPlainDate,
  pickerDateToPlainDateTime,
  pickerDateToZonedDateTime,
  plainDateTimeToPickerDate,
  plainDateToPickerDate,
  zonedDateTimeToPickerDate,
} from './picker-date-conversion';
import { Temporal } from './temporal';

const KYIV = 'Europe/Kyiv';

describe('picker date conversion', () => {
  it('pickerDateToPlainDate_WithDate_KeepsCalendarFields', () => {
    const result = pickerDateToPlainDate(new Date(2026, 10, 3, 23, 30));

    expect(result?.toString()).toBe('2026-11-03');
  });

  it('plainDateTimeToPickerDate_RoundTrip_KeepsWallClock', () => {
    const start = Temporal.PlainDateTime.from('2026-11-03T09:15');

    const result = pickerDateToPlainDateTime(plainDateTimeToPickerDate(start));

    expect(result?.equals(start)).toBe(true);
  });

  it('zonedDateTimeToPickerDate_WithZoneOtherThanRunner_KeepsWallClock', () => {
    const value = Temporal.ZonedDateTime.from('2026-11-03T09:00[America/New_York]');

    const result = zonedDateTimeToPickerDate(value);

    expect(result?.getHours()).toBe(9);
    expect(result?.getMinutes()).toBe(0);
  });

  it('pickerDateToZonedDateTime_InSpringGap_MovesForward', () => {
    const result = pickerDateToZonedDateTime(new Date(2026, 2, 29, 3, 30), KYIV);

    expect(result?.toString()).toBe('2026-03-29T04:30:00+03:00[Europe/Kyiv]');
  });

  it('pickerDateToZonedDateTime_InFallBackOverlap_TakesEarlierOffset', () => {
    const result = pickerDateToZonedDateTime(new Date(2026, 9, 25, 3, 30), KYIV);

    expect(result?.offset).toBe('+03:00');
  });

  it('conversions_WithNull_ReturnNull', () => {
    const results = [
      plainDateToPickerDate(null),
      pickerDateToPlainDate(null),
      plainDateTimeToPickerDate(null),
      pickerDateToPlainDateTime(null),
      zonedDateTimeToPickerDate(null),
      pickerDateToZonedDateTime(null, KYIV),
    ];

    expect(results).toEqual([null, null, null, null, null, null]);
  });
});
