import dstCases from '@testdata/dst-europe-kyiv-2027.json';

import { Temporal } from './temporal';
import { dayRange, isSkipped, toInstant } from './zoned-times';

const ZONE = 'Europe/Kyiv';
const HOUR_MS = 3_600_000;

function parts(local: string): { date: Temporal.PlainDate; time: Temporal.PlainTime } {
  const dateTime = Temporal.PlainDateTime.from(local);
  return { date: dateTime.toPlainDate(), time: dateTime.toPlainTime() };
}

describe('zoned times', () => {
  it.each(dstCases)('toInstant_WithSharedCase_GivesTheUtcValue ($local)', ({ local, utc }) => {
    const { date, time } = parts(local);

    const instant = toInstant(date, time, ZONE);

    expect(instant.toString()).toBe(Temporal.Instant.from(utc).toString());
  });

  it.each(dstCases)('isSkipped_WithSharedCase_GivesTheFlag ($local)', ({ local, isSkipped: expected }) => {
    const { date, time } = parts(local);

    const result = isSkipped(date, time, ZONE);

    expect(result).toBe(expected);
  });

  it.each([
    ['2027-03-28', 23],
    ['2027-10-31', 25],
    ['2027-06-01', 24],
  ])('dayRange_WithDate_HasTheLocalLength (%s)', (day, hours) => {
    const range = dayRange(Temporal.PlainDate.from(day), ZONE);

    expect(range.end.epochMilliseconds - range.start.epochMilliseconds).toBe(hours * HOUR_MS);
  });
});
