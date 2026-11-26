import type { ScheduleBar } from '@shared/schedule/schedule-row';
import { Temporal } from '@shared/time/temporal';

import {
  axisOf,
  canDragBar,
  canResizeBar,
  describeBar,
  gapsOf,
  grabOffsetOf,
  hourLabelsOf,
  isPlaceholderShown,
  isPreviewConflict,
  laneUnderOf,
  leftPxOf,
  lengthMs,
  localRangeOf,
  minutesOfBar,
  nowPercentOf,
  placeBars,
  previewPointOf,
  segmentOf,
  skeletonRowCount,
  snappedStartOf,
  tooltipOf,
  withLiveEnds,
} from './schedule-board.utils';

const ZONE = 'Europe/Kyiv';
const HOUR_MS = 3_600_000;

describe('skeletonRowCount', { tags: ['Platform'] }, () => {
  it.each([
    ['WithRowsShownBefore_KeepsTheirCount', 3, 3],
    ['WithNoRowsShownBefore_UsesTheDefaultCount', 0, 5],
  ])('skeletonRowCount_%s', (_name, shown, expected) => {
    expect(skeletonRowCount(shown)).toBe(expected);
  });
});

function at(local: string): Temporal.Instant {
  return Temporal.PlainDateTime.from(local).toZonedDateTime(ZONE, { disambiguation: 'compatible' }).toInstant();
}

function bar(id: string, start: string, end: string): ScheduleBar {
  return { id, title: id, start: at(start), end: at(end), statusKey: 'S', colorClass: 'c' };
}

const NORMAL_DATE = Temporal.PlainDate.from('2027-06-01');
const NORMAL = axisOf(NORMAL_DATE, ZONE);

describe('hourLabelsOf', { tags: ['Platform'] }, () => {
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

describe('placeBars', { tags: ['Platform'] }, () => {
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

describe('gapsOf', { tags: ['Platform'] }, () => {
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

describe('segmentOf and nowPercentOf', { tags: ['Platform'] }, () => {
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

describe('describeBar', { tags: ['Platform'] }, () => {
  it('describeBar_WithBar_GivesLocalTimesAndTheOffset', () => {
    const text = describeBar(bar('a', '2027-06-01T09:00', '2027-06-01T10:30'), ZONE);

    expect(text).toBe('09:00 - 10:30 (+03:00)');
  });
});

describe('tooltipOf', { tags: ['Platform'] }, () => {
  it('tooltipOf_WithBar_GivesTheTitleThenStatusRangeAndOffsetOnTwoLines', () => {
    const text = tooltipOf(bar('a', '2027-06-01T09:00', '2027-06-01T10:30'), 'Staged', ZONE);

    expect(text.split('\n')).toEqual(['a', 'Staged · 09:00 - 10:30 (+03:00)']);
  });
});

describe('bars across days', { tags: ['Platform'] }, () => {
  const running = (start: string, plannedEnd: string, end: string): ScheduleBar => ({
    ...bar('r', start, end),
    isRunning: true,
    plannedEnd: at(plannedEnd),
  });

  describe('withLiveEnds', () => {
    it.each([
      ['ARunningBarPastItsEnd_EndsItAtNow', '2027-06-01T10:00', '2027-06-01T12:00', '2027-06-01T12:00'],
      ['ARunningBarBeforeItsEnd_KeepsTheEnd', '2027-06-01T11:00', '2027-06-01T10:00', '2027-06-01T11:00'],
    ])('withLiveEnds_For%s', (_name, end, now, expected) => {
      const [live] = withLiveEnds([running('2027-06-01T09:00', end, end)], at(now));

      expect(live.end.equals(at(expected))).toBe(true);
    });

    it('withLiveEnds_ForABarThatIsNotRunning_KeepsTheEnd', () => {
      const [live] = withLiveEnds([bar('a', '2027-06-01T09:00', '2027-06-01T10:00')], at('2027-06-01T12:00'));

      expect(live.end.equals(at('2027-06-01T10:00'))).toBe(true);
    });
  });

  describe('placeBars cut flags and locks', () => {
    it.each([
      ['StartsBeforeTheDay', '2027-05-31T22:00', '2027-06-01T02:00', true, false, true, false],
      ['EndsAfterTheDay', '2027-06-01T22:00', '2027-06-02T02:00', false, true, false, false],
      ['InsideTheDay', '2027-06-01T09:00', '2027-06-01T10:00', false, false, true, true],
    ])(
      'placeBars_ForABar%s_SetsTheCutFlagsAndTheResizeAndDragRights',
      (_name, start, end, isCutStart, isCutEnd, canResize, canDrag) => {
        const { placed } = placeBars([bar('a', start, end)], NORMAL);

        expect([
          placed[0].isCutStart,
          placed[0].isCutEnd,
          canResizeBar(placed[0], NORMAL),
          canDragBar(placed[0]),
        ]).toEqual([isCutStart, isCutEnd, canResize, canDrag]);
      },
    );

    it('canResizeBar_ForACompletedBarInsideTheDay_IsFalse', () => {
      const { placed } = placeBars([{ ...bar('a', '2027-06-01T09:00', '2027-06-01T10:00'), isLocked: true }], NORMAL);

      expect(canResizeBar(placed[0], NORMAL)).toBe(false);
    });

    it.each([
      ['Staged', {}, true, true],
      ['InProgress', { isRunning: true }, false, true],
      ['Completed', { isLocked: true }, false, false],
    ])('canDragBar_ForA%sBar_GivesTheDragAndTheResizeRights', (_name, flags, canDrag, canResize) => {
      const { placed } = placeBars([{ ...bar('a', '2027-06-01T09:00', '2027-06-01T10:00'), ...flags }], NORMAL);

      expect([canDragBar(placed[0]), canResizeBar(placed[0], NORMAL)]).toEqual([canDrag, canResize]);
    });

    it('canResizeBar_ForACutRunningBarWithItsPlannedEndOnTheDay_IsTrueAndItDoesNotDrag', () => {
      const { placed } = placeBars([running('2027-05-31T22:00', '2027-06-01T01:00', '2027-06-01T02:00')], NORMAL);

      expect([canDragBar(placed[0]), canResizeBar(placed[0], NORMAL)]).toEqual([false, true]);
    });

    it('canResizeBar_ForABarCutAtTheEndWithItsOverrunStartOnTheDay_IsTrueAndItDoesNotDrag', () => {
      const { placed } = placeBars([running('2027-06-01T08:00', '2027-06-01T09:00', '2027-06-02T02:00')], NORMAL);

      expect([placed[0].isCutEnd, canDragBar(placed[0]), canResizeBar(placed[0], NORMAL)]).toEqual([true, false, true]);
    });

    it('canResizeBar_ForARunningBarWithItsPlannedEndOffTheDay_IsFalse', () => {
      const { placed } = placeBars([running('2027-05-31T08:00', '2027-05-31T09:00', '2027-06-01T02:00')], NORMAL);

      expect(canResizeBar(placed[0], NORMAL)).toBe(false);
    });

    it('canResizeBar_ForABarCutAtTheEndWithItsEndOffTheDay_IsFalse', () => {
      const { placed } = placeBars([bar('a', '2027-06-01T22:00', '2027-06-02T02:00')], NORMAL);

      expect(canResizeBar(placed[0], NORMAL)).toBe(false);
    });
  });

  describe('overrun part', () => {
    it('placeBars_ForARunningBarPastItsPlannedEnd_GivesTheOverrunStartInPercentOfTheBar', () => {
      const { placed } = placeBars([running('2027-06-01T09:00', '2027-06-01T10:00', '2027-06-01T13:00')], NORMAL);

      expect(placed[0].overrunStartPercent).toBeCloseTo(25);
    });

    it('placeBars_ForARunningBarCutAtTheStartAndPastItsPlannedEnd_StartsTheOverrunAtTheLeftEdge', () => {
      const { placed } = placeBars([running('2027-05-31T20:00', '2027-05-31T22:00', '2027-06-01T02:00')], NORMAL);

      expect(placed[0].overrunStartPercent).toBe(0);
    });

    it('placeBars_ForARunningBarBeforeItsPlannedEnd_HasNoOverrun', () => {
      const { placed } = placeBars([running('2027-06-01T09:00', '2027-06-01T11:00', '2027-06-01T10:00')], NORMAL);

      expect(placed[0].overrunStartPercent).toBeNull();
    });
  });

  describe('describeBar with dates', () => {
    it.each([
      [
        'ABarThatStartsOnAnotherDate_ShowsBothDates',
        '2027-05-31T22:00',
        '2027-06-01T02:00',
        '2027-05-31 22:00 - 2027-06-01 02:00 (+03:00)',
      ],
      ['ABarInsideTheDate_ShowsOnlyTheTimes', '2027-06-01T09:00', '2027-06-01T10:00', '09:00 - 10:00 (+03:00)'],
    ])('describeBar_For%s', (_name, start, end, expected) => {
      const text = describeBar(bar('a', start, end), ZONE, Temporal.PlainDate.from('2027-06-01'));

      expect(text).toBe(expected);
    });
  });
});

describe('preview snapping', { tags: ['Platform'] }, () => {
  const AREA = { left: 100, width: 2400 };
  const ROOT_FONT_PX = 16;

  it.each([
    ['OnAGridStep', 100 + 9 * 100, '2027-06-01T09:00'],
    ['BetweenGridSteps', 100 + 905, '2027-06-01T09:05'],
    ['JustBeforeTheNextStep', 100 + 907, '2027-06-01T09:05'],
    ['LeftOfTheAxis', 100 - 100, '2027-05-31T23:00'],
  ])('snappedStartOf_ForAPreviewLeftEdge%s_SnapsToTheGrid', (_name, leftPx, local) => {
    expect(snappedStartOf(leftPx, AREA, NORMAL)?.equals(at(local))).toBe(true);
  });

  it('snappedStartOf_ForAnEmptyAxis_IsNull', () => {
    expect(snappedStartOf(10, { left: 0, width: 0 }, NORMAL)).toBeNull();
  });

  it('leftPxOf_ForTheSnappedStart_GivesTheEdgeOfThePreview', () => {
    expect(leftPxOf(at('2027-06-01T09:05'), AREA, NORMAL)).toBeCloseTo(100 + ((9 * 60 + 5) / 60) * 100, 5);
  });

  it.each([
    ['AbovePadding', 0, 0],
    ['FirstLane', 15, 0],
    ['SecondLane', 15 + 28, 1],
    ['BelowTheLastLane', 500, 1],
  ])('laneUnderOf_ForThePointer%s_GivesTheLane', (_name, offsetPx, lane) => {
    expect(laneUnderOf(1000 + offsetPx, 1000, 2, ROOT_FONT_PX)).toBe(lane);
  });

  it.each([
    ['OverARow', { leftPx: 1005, laneTopPx: 1015 }, { x: 1005, y: 1015 }],
    ['OutsideEveryRow', null, { x: 970, y: 1000 - 12 }],
  ])('previewPointOf_For%s_GivesTheTopLeftOfThePreview', (_name, slot, point) => {
    expect(previewPointOf(slot, { x: 1000, y: 1000 }, 30, ROOT_FONT_PX)).toEqual(point);
  });

  describe('localRangeOf', () => {
    it('localRangeOf_ForASlotOnTheShownDay_GivesTheTimesOnly', () => {
      expect(localRangeOf(at('2027-06-01T09:05'), at('2027-06-01T10:35'), ZONE, NORMAL_DATE)).toBe('09:05 - 10:35');
    });

    it('localRangeOf_ForASlotThatEndsOnTheNextDay_GivesBothDates', () => {
      expect(localRangeOf(at('2027-06-01T23:00'), at('2027-06-02T01:00'), ZONE, NORMAL_DATE)).toBe(
        '2027-06-01 23:00 - 2027-06-02 01:00',
      );
    });
  });

  describe('isPreviewConflict', () => {
    const blocks = [{ start: at('2027-06-01T08:00'), end: at('2027-06-01T17:00') }];
    const others = [{ start: at('2027-06-01T10:00'), end: at('2027-06-01T11:00') }];
    const now = at('2027-06-01T08:30');

    it.each([
      ['FitsInABlockWithoutOverlap', '2027-06-01T08:30', '2027-06-01T09:30', false],
      ['TouchesAnotherBar', '2027-06-01T09:00', '2027-06-01T10:00', false],
      ['OverlapsAnotherBar', '2027-06-01T09:30', '2027-06-01T10:30', true],
      ['LeavesEveryBlock', '2027-06-01T16:30', '2027-06-01T17:30', true],
      ['StartsBeforeNow', '2027-06-01T08:00', '2027-06-01T09:00', true],
    ])('isPreviewConflict_ForASlotThat%s_Decides', (_name, start, end, isConflict) => {
      expect(isPreviewConflict({ start: at(start), end: at(end) }, blocks, others, now)).toBe(isConflict);
    });
  });

  it('minutesOfBar_ForABarOfOneAndAHalfHours_Gives90', () => {
    expect(minutesOfBar(bar('a', '2027-06-01T09:00', '2027-06-01T10:30'))).toBe(90);
  });
});

describe('grabOffsetOf', { tags: ['Platform'] }, () => {
  it.each([
    ['InsideTheElement', 130, 30],
    ['LeftOfTheElement', 90, 0],
    ['RightOfTheElement', 400, 200],
  ])('grabOffsetOf_ForAPointer%s_IsClampedToTheElement', (_name, clientX, offset) => {
    expect(grabOffsetOf(clientX, { left: 100, width: 200 })).toBe(offset);
  });
});

describe('isPlaceholderShown', { tags: ['Platform'] }, () => {
  const source = Temporal.PlainDate.from('2027-06-01');

  it.each([
    ['NoDragRuns', null, Temporal.PlainDate.from('2027-06-02'), true],
    ['TheSourceDayIsShown', source, Temporal.PlainDate.from('2027-06-01'), true],
    ['AnotherDayIsShown', source, Temporal.PlainDate.from('2027-06-02'), false],
  ])('isPlaceholderShown_When%s_Decides', (_name, sourceDate, shownDate, isShown) => {
    expect(isPlaceholderShown(shownDate, sourceDate)).toBe(isShown);
  });
});
