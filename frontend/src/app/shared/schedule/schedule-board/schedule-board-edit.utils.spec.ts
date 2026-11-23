import { Temporal } from '@shared/time/temporal';

import {
  areDayAreasShown,
  axisOf,
  barLeftCss,
  barTopCss,
  barWidthCss,
  columnWidthOf,
  createClickGuard,
  createDwell,
  DAY_SWITCH_DWELL_MS,
  dropOutcomeOf,
  dropStartOf,
  dropTargetOf,
  instantAt,
  isSkeletonShown,
  minutesOfWidth,
  ROW_ATTRIBUTE,
  rowHeightRem,
  shownHourLabels,
  shownWhileHolding,
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

    it('dropTargetOf_WithTheBacklogButtonUnderThePointer_GivesStaging', () => {
      const button = document.createElement('button');
      button.setAttribute(STAGING_ATTRIBUTE, '');

      expect(dropTargetOf([button])).toEqual({ kind: 'staging' });
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

  describe('dropStartOf', () => {
    const axis = axisOf(Temporal.PlainDate.from('2027-06-01'), ZONE);
    const area = { left: 100, width: 2400 };
    const now = Temporal.Instant.from('2027-06-01T05:00:00Z');

    it('dropStartOf_ForAPreviewLeftEdgeOnTheAxis_GivesTheSnappedStart', () => {
      const start = dropStartOf(100 + 1013, area, axis, now);

      expect(start === null ? null : localTime(start)).toBe('2027-06-01T10:10');
    });

    it('dropStartOf_ForALeftEdgeBeforeNow_GivesNothing', () => {
      expect(dropStartOf(100 + 200, area, axis, now)).toBeNull();
    });

    it('dropStartOf_ForAnAxisWithNoWidth_GivesNothing', () => {
      expect(dropStartOf(100, { left: 100, width: 0 }, axis, now)).toBeNull();
    });
  });

  describe('shownHourLabels', () => {
    it('shownHourLabels_ForColumnsAsWideAsTheLabel_ShowsEveryLabel', () => {
      expect(shownHourLabels(4, 40, 40)).toEqual([true, true, true, true]);
    });

    it('shownHourLabels_ForNarrowColumns_ShowsOnlyTheEvenHours', () => {
      expect(shownHourLabels(4, 39, 40)).toEqual([true, false, true, false]);
    });
  });

  describe('columnWidthOf', () => {
    it('columnWidthOf_ForABoardWithTheNameColumn_DividesTheRest', () => {
      expect(columnWidthOf(1360, 160, 24)).toBe(50);
    });

    it('columnWidthOf_ForNoHours_IsZero', () => {
      expect(columnWidthOf(1360, 160, 0)).toBe(0);
    });
  });

  describe('barLeftCss and barWidthCss', () => {
    it('barLeftCss_ForASlot_StartsOneInsetInsideIt', () => {
      expect(barLeftCss(25)).toBe('calc(25% + 0.0625rem)');
    });

    it('barWidthCss_ForASlot_LeavesAnInsetOnBothSides', () => {
      expect(barWidthCss(10)).toBe('calc(10% - 0.125rem)');
    });
  });
});

describe('drop and drag decisions', () => {
  const axis = axisOf(Temporal.PlainDate.from('2027-06-01'), ZONE);
  const area = { left: 100, width: 2400 };
  const now = Temporal.Instant.from('2027-06-01T05:00:00Z');
  const row = { kind: 'row', rowId: 'e1' } as const;
  const staging = { kind: 'staging' } as const;

  describe('dropOutcomeOf', () => {
    it('dropOutcomeOf_ForABoardBarOnTheBacklog_Unassigns', () => {
      expect(dropOutcomeOf(staging, true, 0, area, axis, now)).toEqual({ kind: 'unassign' });
    });

    it('dropOutcomeOf_ForABacklogCardOnTheBacklog_DoesNothing', () => {
      expect(dropOutcomeOf(staging, false, 0, area, axis, now)).toBeNull();
    });

    it('dropOutcomeOf_ForABacklogCardOnARow_PlacesItAtTheSnappedStart', () => {
      const outcome = dropOutcomeOf(row, false, 100 + 1013, area, axis, now);

      expect(outcome?.kind).toBe('place');
      expect(outcome?.kind === 'place' ? [outcome.rowId, localTime(outcome.start)] : null).toEqual([
        'e1',
        '2027-06-01T10:10',
      ]);
    });

    it('dropOutcomeOf_ForARowAndAStartBeforeNow_DoesNothing', () => {
      expect(dropOutcomeOf(row, true, 100 + 200, area, axis, now)).toBeNull();
    });

    it('dropOutcomeOf_ForNothingUnderThePointer_DoesNothing', () => {
      expect(dropOutcomeOf(null, true, 0, area, axis, now)).toBeNull();
    });
  });

  describe('shownWhileHolding', () => {
    it('shownWhileHolding_WhileADragHoldsTheBoard_KeepsThePreviousRows', () => {
      expect(shownWhileHolding(['new'], ['old'], true)).toEqual(['old']);
    });

    it('shownWhileHolding_WithoutADrag_ShowsTheNextRows', () => {
      expect(shownWhileHolding(['new'], ['old'], false)).toEqual(['new']);
    });

    it('shownWhileHolding_WithNoPreviousRows_ShowsTheNextRows', () => {
      expect(shownWhileHolding(['new'], undefined, true)).toEqual(['new']);
    });
  });

  describe('isSkeletonShown', () => {
    it.each([
      ['LoadingWithoutADrag', true, false, true],
      ['LoadingDuringADrag', true, true, false],
      ['NotLoading', false, false, false],
    ])('isSkeletonShown_For%s_Decides', (_name, isLoading, isDragActive, expected) => {
      expect(isSkeletonShown(isLoading, isDragActive)).toBe(expected);
    });
  });

  describe('areDayAreasShown', () => {
    it.each([
      ['BoardDrag', true, true, false, true],
      ['BacklogDrag', true, false, true, true],
      ['NoDrag', true, false, false, false],
      ['ReadOnlyBoard', false, true, true, false],
    ])('areDayAreasShown_For%s_Decides', (_name, isEditable, isDragging, isExternal, expected) => {
      expect(areDayAreasShown(isEditable, isDragging, isExternal)).toBe(expected);
    });
  });

  describe('createClickGuard', () => {
    afterEach(() => vi.useRealTimers());

    it('createClickGuard_AfterADrag_BlocksTheClickUntilTheTaskEnds', async () => {
      vi.useFakeTimers();
      const guard = createClickGuard();

      guard.markDragged();
      const isBlockedAfterDrag = guard.isBlocked();
      await vi.advanceTimersByTimeAsync(1);

      expect(isBlockedAfterDrag).toBe(true);
      expect(guard.isBlocked()).toBe(false);
    });
  });
});
