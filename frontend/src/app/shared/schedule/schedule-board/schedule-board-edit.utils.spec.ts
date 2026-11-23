import { Temporal } from '@shared/time/temporal';

import {
  axisOf,
  barTopCss,
  createDwell,
  DAY_SWITCH_DWELL_MS,
  dropTargetOf,
  instantAt,
  minutesOfWidth,
  ROW_ATTRIBUTE,
  rowHeightRem,
  snapToGrid,
  STAGING_ATTRIBUTE,
  widthPercentOf,
} from './schedule-board.utils';

const ZONE = 'Europe/Kyiv';

function localTime(instant: Temporal.Instant): string {
  return instant.toZonedDateTimeISO(ZONE).toPlainDateTime().toString({ smallestUnit: 'minute' });
}

describe('schedule board edit utils', () => {
  describe('instantAt and snapToGrid', () => {
    it.each([
      ['NormalDay', '2027-06-01', 37.5, '2027-06-01T09:00'],
      ['SpringForwardDay', '2027-03-28', 50, '2027-03-28T12:30'],
      ['FallBackDay', '2027-10-31', 50, '2027-10-31T11:30'],
    ])('instantAt_On%s_GivesTheLocalTimeOfThePosition', (_name, date, percent, expected) => {
      const axis = axisOf(Temporal.PlainDate.from(date), ZONE);

      const instant = snapToGrid(instantAt(percent, axis));

      expect(localTime(instant)).toBe(expected);
    });

    it.each([
      ['2027-06-01T06:02:29Z', '2027-06-01T06:00:00Z'],
      ['2027-06-01T06:02:31Z', '2027-06-01T06:05:00Z'],
    ])('snapToGrid_Of%s_Is%s', (instant, expected) => {
      expect(snapToGrid(Temporal.Instant.from(instant)).toString()).toBe(expected);
    });
  });

  describe('minutesOfWidth', () => {
    const axis = axisOf(Temporal.PlainDate.from('2027-06-01'), ZONE);

    it.each([
      ['OneHour', widthPercentOf(60, axis), 60],
      ['SnappedToTheGrid', widthPercentOf(62, axis), 60],
      ['BelowTheMinimum', 0, 5],
      ['AboveTheMaximum', 90, 720],
    ])('minutesOfWidth_With%s_Is%s', (_name, width, expected) => {
      expect(minutesOfWidth(width, axis)).toBe(expected);
    });

    it('minutesOfWidth_OnTheSpringForwardDay_UsesTheRealDayLength', () => {
      const shortDay = axisOf(Temporal.PlainDate.from('2027-03-28'), ZONE);

      expect(minutesOfWidth(widthPercentOf(60, shortDay), shortDay)).toBe(60);
    });
  });

  describe('createDwell', () => {
    afterEach(() => vi.useRealTimers());

    it('createDwell_AfterTheDwellTime_RunsTheCallbackOnceAndWaitsAgain', () => {
      vi.useFakeTimers();
      const elapsed = vi.fn();
      const dwell = createDwell(elapsed);

      dwell.enter();
      vi.advanceTimersByTime(DAY_SWITCH_DWELL_MS - 1);
      const before = elapsed.mock.calls.length;
      vi.advanceTimersByTime(1);
      const afterFirst = elapsed.mock.calls.length;
      vi.advanceTimersByTime(DAY_SWITCH_DWELL_MS);

      expect([before, afterFirst, elapsed.mock.calls.length]).toEqual([0, 1, 2]);
      dwell.leave();
    });

    it('createDwell_WhenThePointerLeavesEarly_NeverRunsTheCallback', () => {
      vi.useFakeTimers();
      const elapsed = vi.fn();
      const dwell = createDwell(elapsed);

      dwell.enter();
      vi.advanceTimersByTime(DAY_SWITCH_DWELL_MS - 1);
      dwell.leave();
      vi.advanceTimersByTime(DAY_SWITCH_DWELL_MS * 2);

      expect(elapsed).not.toHaveBeenCalled();
    });

    it('DAY_SWITCH_DWELL_MS_IsSixHundredMilliseconds', () => {
      expect(DAY_SWITCH_DWELL_MS).toBe(600);
    });
  });

  describe('dropTargetOf', () => {
    function tagged(attribute: string, value: string): HTMLElement {
      const element = document.createElement('div');
      element.setAttribute(attribute, value);
      const child = document.createElement('span');
      element.appendChild(child);
      return child;
    }

    it('dropTargetOf_WithARowUnderThePointer_GivesTheRow', () => {
      expect(dropTargetOf([document.createElement('i'), tagged(ROW_ATTRIBUTE, 'e1')])).toEqual({
        kind: 'row',
        rowId: 'e1',
      });
    });

    it('dropTargetOf_WithTheStagingAreaUnderThePointer_GivesStaging', () => {
      expect(dropTargetOf([tagged(STAGING_ATTRIBUTE, '')])).toEqual({ kind: 'staging' });
    });

    it('dropTargetOf_WithNothingTagged_GivesNull', () => {
      expect(dropTargetOf([document.createElement('i')])).toBeNull();
    });
  });

  describe('barTopCss', () => {
    it('rowHeightRem_ForTwoLanes_AddsThePaddingTheLanesAndTheGap', () => {
      expect(rowHeightRem(2)).toBe(2 * 0.9375 + 2 * 1.5 + 0.25);
    });

    it('barTopCss_ForTheSecondLaneOfTheSecondRow_SumsTheRowsAboveAndTheLaneOffset', () => {
      expect(barTopCss([2, 1], 1, 0)).toBe(`calc(${rowHeightRem(2) + 0.9375}rem + 1px)`);
    });
  });
});
