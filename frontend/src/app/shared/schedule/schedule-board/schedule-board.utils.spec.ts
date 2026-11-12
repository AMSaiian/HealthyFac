import type { ScheduleBar } from '@shared/schedule/schedule-row';
import { Temporal } from '@shared/time/temporal';

import {
  axisOf,
  describeBar,
  gapsOf,
  hourLabelsOf,
  lengthMs,
  nowPercentOf,
  placeBars,
  segmentOf,
  skeletonRowCount,
  tooltipOf,
} from './schedule-board.utils';

const ZONE = 'Europe/Kyiv';
const HOUR_MS = 3_600_000;

describe('skeletonRowCount', () => {
  it('skeletonRowCount_WithRowsShownBefore_KeepsTheirCount', () => {
    expect(skeletonRowCount(3)).toBe(3);
  });

  it('skeletonRowCount_WithNoRowsShownBefore_UsesTheDefaultCount', () => {
    expect(skeletonRowCount(0)).toBe(5);
  });
});

function at(local: string): Temporal.Instant {
  return Temporal.PlainDateTime.from(local).toZonedDateTime(ZONE, { disambiguation: 'compatible' }).toInstant();
}

function bar(id: string, start: string, end: string): ScheduleBar {
  return { id, title: id, start: at(start), end: at(end), statusKey: 'S', colorClass: 'c' };
}

const NORMAL = axisOf(Temporal.PlainDate.from('2027-06-01'), ZONE);

describe('hourLabelsOf', () => {
  it('hourLabelsOf_OnANormalDay_Gives24HoursFrom0000To2300', () => {
    const labels = hourLabelsOf(NORMAL, ZONE);

    expect(labels).toHaveLength(24);
    expect(labels[0]).toBe('00:00');
    expect(labels[23]).toBe('23:00');
  });

  it('hourLabelsOf_OnTheSpringChangeDay_Gives23HoursWithoutTheSkippedHour', () => {
    const axis = axisOf(Temporal.PlainDate.from('2027-03-28'), ZONE);

    const labels = hourLabelsOf(axis, ZONE);

    expect(lengthMs(axis) / HOUR_MS).toBe(23);
    expect(labels).toHaveLength(23);
    expect(labels).not.toContain('03:00');
  });

  it('hourLabelsOf_OnTheAutumnChangeDay_Gives25HoursWithTheRepeatedHourTwice', () => {
    const axis = axisOf(Temporal.PlainDate.from('2027-10-31'), ZONE);

    const labels = hourLabelsOf(axis, ZONE);

    expect(lengthMs(axis) / HOUR_MS).toBe(25);
    expect(labels).toHaveLength(25);
    expect(labels.filter((label) => label === '03:00')).toHaveLength(2);
  });
});

describe('placeBars', () => {
  it('placeBars_WithOneBar_SitsAtThePercentOfItsInstants', () => {
    const { placed } = placeBars([bar('a', '2027-06-01T06:00', '2027-06-01T12:00')], NORMAL);

    expect(placed[0].left).toBeCloseTo(25);
    expect(placed[0].width).toBeCloseTo(25);
    expect(placed[0].lane).toBe(0);
  });

  it('placeBars_WithTwoOverlappingBars_GivesTwoLanes', () => {
    const { placed, laneCount } = placeBars(
      [bar('a', '2027-06-01T08:00', '2027-06-01T10:00'), bar('b', '2027-06-01T09:00', '2027-06-01T11:00')],
      NORMAL,
    );

    expect(laneCount).toBe(2);
    expect(placed.map((item) => item.lane)).toEqual([0, 1]);
  });

  it('placeBars_WithBarsThatTouch_ShareOneLane', () => {
    const { laneCount } = placeBars(
      [bar('a', '2027-06-01T08:00', '2027-06-01T09:00'), bar('b', '2027-06-01T09:00', '2027-06-01T10:00')],
      NORMAL,
    );

    expect(laneCount).toBe(1);
  });

  it('placeBars_WithBarPastTheDayEnd_ClipsItAtTheAxisEnd', () => {
    const { placed } = placeBars([bar('a', '2027-06-01T23:00', '2027-06-02T02:00')], NORMAL);

    expect(placed[0].left + placed[0].width).toBeCloseTo(100);
  });

  it('placeBars_WithBarOutsideTheDay_LeavesItOut', () => {
    const { placed, laneCount } = placeBars([bar('a', '2027-06-02T08:00', '2027-06-02T09:00')], NORMAL);

    expect(placed).toEqual([]);
    expect(laneCount).toBe(1);
  });
});

describe('gapsOf', () => {
  it('gapsOf_WithoutBlocks_ShadesTheWholeDay', () => {
    const gaps = gapsOf([], NORMAL);

    expect(gaps).toEqual([{ left: 0, width: 100 }]);
  });

  it('gapsOf_WithTwoBlocks_ShadesBeforeBetweenAndAfter', () => {
    const gaps = gapsOf(
      [
        { start: at('2027-06-01T08:00'), end: at('2027-06-01T12:00') },
        { start: at('2027-06-01T13:00'), end: at('2027-06-01T17:00') },
      ],
      NORMAL,
    );

    expect(gaps).toHaveLength(3);
    expect(gaps[0].left).toBe(0);
    expect(gaps[1].width).toBeCloseTo(100 / 24);
    expect(gaps[2].left + gaps[2].width).toBeCloseTo(100);
  });

  it('gapsOf_WithBlockFillingTheDay_HasNoGap', () => {
    const gaps = gapsOf([{ start: NORMAL.start, end: NORMAL.end }], NORMAL);

    expect(gaps).toEqual([]);
  });
});

describe('segmentOf and nowPercentOf', () => {
  it('segmentOf_WithEmptyRange_GivesNull', () => {
    const segment = segmentOf(at('2027-06-01T08:00'), at('2027-06-01T08:00'), NORMAL);

    expect(segment).toBeNull();
  });

  it('nowPercentOf_WithNowOnTheDay_GivesThePercent', () => {
    const percent = nowPercentOf(at('2027-06-01T12:00'), NORMAL);

    expect(percent).toBeCloseTo(50);
  });

  it('nowPercentOf_WithNowOnAnotherDay_GivesNull', () => {
    const percent = nowPercentOf(at('2027-06-02T12:00'), NORMAL);

    expect(percent).toBeNull();
  });
});

describe('describeBar', () => {
  it('describeBar_WithBar_GivesLocalTimesAndTheOffset', () => {
    const text = describeBar(bar('a', '2027-06-01T09:00', '2027-06-01T10:30'), ZONE);

    expect(text).toBe('09:00 - 10:30 (+03:00)');
  });
});

describe('tooltipOf', () => {
  it('tooltipOf_WithBar_GivesTheTitleThenStatusRangeAndOffsetOnTwoLines', () => {
    const text = tooltipOf(bar('a', '2027-06-01T09:00', '2027-06-01T10:30'), 'Staged', ZONE);

    expect(text.split('\n')).toEqual(['a', 'Staged · 09:00 - 10:30 (+03:00)']);
  });
});
