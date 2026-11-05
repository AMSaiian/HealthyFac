import {
  pickerDatesToPlainDateRange,
  pickerDateToPlainDate,
  pickerDateToPlainDateTime,
  pickerDateToZonedDateTime,
  plainDateRangeToPickerDates,
  plainDateTimeToPickerDate,
  plainDateToPickerDate,
  zonedDateTimeToPickerDate,
} from './picker-date-conversion';
import { Temporal } from '@shared/time/temporal';

const KYIV = 'Europe/Kyiv';

describe('picker date range conversion', () => {
  it('pickerDatesToPlainDateRange_WithTwoDates_GivesFromAndTo', () => {
    const range = pickerDatesToPlainDateRange([new Date(2026, 10, 3, 18), new Date(2026, 10, 9, 2)]);

    expect(range.from?.toString()).toBe('2026-11-03');
    expect(range.to?.toString()).toBe('2026-11-09');
  });

  it('pickerDatesToPlainDateRange_WithOneDate_GivesOpenEnd', () => {
    const withNull = pickerDatesToPlainDateRange([new Date(2026, 10, 3), null]);
    const withOneItem = pickerDatesToPlainDateRange([new Date(2026, 10, 3)]);

    expect(withNull.from?.toString()).toBe('2026-11-03');
    expect(withNull.to).toBeNull();
    expect(withOneItem.to).toBeNull();
  });

  it('pickerDatesToPlainDateRange_WithoutDates_GivesEmptyRange', () => {
    const fromNull = pickerDatesToPlainDateRange(null);
    const fromEmpty = pickerDatesToPlainDateRange([]);

    expect(fromNull).toEqual({ from: null, to: null });
    expect(fromEmpty).toEqual({ from: null, to: null });
  });

  it('plainDateRangeToPickerDates_RoundTrip_KeepsDays', () => {
    const range = { from: Temporal.PlainDate.from('2026-11-03'), to: Temporal.PlainDate.from('2026-11-09') };

    const result = pickerDatesToPlainDateRange(plainDateRangeToPickerDates(range));

    expect(result.from?.equals(range.from)).toBe(true);
    expect(result.to?.equals(range.to)).toBe(true);
  });

  it('plainDateRangeToPickerDates_WithEmptyRange_GivesNull', () => {
    const dates = plainDateRangeToPickerDates({ from: null, to: null });

    expect(dates).toBeNull();
  });

  it('plainDateRangeToPickerDates_WithOpenEnd_KeepsStartOnly', () => {
    const dates = plainDateRangeToPickerDates({ from: Temporal.PlainDate.from('2026-11-03'), to: null });

    expect(dates).toEqual([new Date(2026, 10, 3), null]);
  });
});

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
