import { Temporal } from '@shared/time/temporal';

import type { FreeSpan, ScheduleDay } from '@work-orders/work-order';

import { DayLabel, dayLabelOf, shownRange, SpanState, spanStateOf, startBounds } from './assignment-picker.utils';

const SUNDAY = 0;
const MONDAY = 1;

function day(hasBlocks: boolean, longestFreeMinutes: number): ScheduleDay {
  return { date: '2027-06-01', hasBlocks, longestFreeMinutes };
}

function span(startLocal: string, endLocal: string, startUtc: string, endUtc: string): FreeSpan {
  return { startLocal, endLocal, startUtc, endUtc };
}

const AFTERNOON = span('2027-06-01T13:00:00', '2027-06-01T17:00:00', '2027-06-01T10:00:00Z', '2027-06-01T14:00:00Z');

describe('dayLabelOf', () => {
  it.each([
    { name: 'noBlocks', hasBlocks: false, free: 0, expected: DayLabel.DayOff },
    { name: 'blocksWithoutFreeTime', hasBlocks: true, free: 0, expected: DayLabel.Busy },
    { name: 'freeBelowEstimate', hasBlocks: true, free: 59, expected: DayLabel.TooShort },
    { name: 'freeEqualToEstimate', hasBlocks: true, free: 60, expected: DayLabel.Fits },
    { name: 'freeAboveEstimate', hasBlocks: true, free: 240, expected: DayLabel.Fits },
  ])('dayLabelOf_$name', ({ hasBlocks, free, expected }) => {
    expect(dayLabelOf(day(hasBlocks, free), 60)).toBe(expected);
  });
});

describe('shownRange', () => {
  it('shownRange_MonthStartingOnMonday_StartsOnItsFirstDayWithMondayAsFirstWeekday', () => {
    const range = shownRange(Temporal.PlainYearMonth.from('2027-02'), MONDAY);

    expect(range.from.toString()).toBe('2027-02-01');
    expect(range.to.toString()).toBe('2027-03-14');
  });

  it('shownRange_MonthStartingOnSunday_StartsOnTheMondayBeforeItWhenMondayIsFirst', () => {
    const range = shownRange(Temporal.PlainYearMonth.from('2027-08'), MONDAY);

    expect(range.from.toString()).toBe('2027-07-26');
    expect(range.to.toString()).toBe('2027-09-05');
  });

  it('shownRange_MonthStartingOnSunday_StartsOnItsFirstDayWhenSundayIsFirst', () => {
    const range = shownRange(Temporal.PlainYearMonth.from('2027-08'), SUNDAY);

    expect(range.from.toString()).toBe('2027-08-01');
  });
});

describe('spanStateOf', () => {
  it.each([
    { name: 'equal', estimate: 240, expected: SpanState.Fits },
    { name: 'longer', estimate: 120, expected: SpanState.Fits },
    { name: 'shorter', estimate: 245, expected: SpanState.TooShort },
  ])('spanStateOf_SpanOfFourHours_$name', ({ estimate, expected }) => {
    expect(spanStateOf(AFTERNOON, estimate)).toBe(expected);
  });
});

describe('startBounds', () => {
  it('startBounds_SpanOfFourHoursAndTwoHourEstimate_GivesThirteenToFifteen', () => {
    const bounds = startBounds(AFTERNOON, 120);

    expect(bounds?.earliest.toString()).toBe('13:00:00');
    expect(bounds?.latest.toString()).toBe('15:00:00');
  });

  it('startBounds_SpanEqualToTheEstimate_GivesOneStart', () => {
    const bounds = startBounds(AFTERNOON, 240);

    expect(bounds?.earliest.toString()).toBe('13:00:00');
    expect(bounds?.latest.toString()).toBe('13:00:00');
  });

  it('startBounds_SpanShorterThanTheEstimate_GivesNull', () => {
    expect(startBounds(AFTERNOON, 300)).toBeNull();
  });

  it('startBounds_SpanPastMidnight_EndsOnTheLastGridStepOfTheFirstDay', () => {
    const night = span('2027-06-01T22:00:00', '2027-06-02T06:00:00', '2027-06-01T19:00:00Z', '2027-06-02T03:00:00Z');

    expect(startBounds(night, 60)?.latest.toString()).toBe('23:55:00');
  });
});
