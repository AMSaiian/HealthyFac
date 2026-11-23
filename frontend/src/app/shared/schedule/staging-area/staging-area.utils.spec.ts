import { axisOf } from '@shared/schedule/schedule-board/schedule-board.utils';
import { Temporal } from '@shared/time/temporal';

import { chipTooltipOf, estimateTextOf, previewWidthOf } from './staging-area.utils';

const AXIS = axisOf(Temporal.PlainDate.from('2027-06-01'), 'Europe/Kyiv');

describe('estimateTextOf', () => {
  it.each([
    ['LessThanOneHour', 45, 'WORK_ORDERS.SCHEDULE_TAB.ESTIMATE_MINUTES', 0, 45],
    ['ExactHours', 120, 'WORK_ORDERS.SCHEDULE_TAB.ESTIMATE_HOURS', 2, 0],
    ['HoursAndMinutes', 90, 'WORK_ORDERS.SCHEDULE_TAB.ESTIMATE_HOURS_MINUTES', 1, 30],
  ])('estimateTextOf_For%s_GivesTheKeyAndTheParts', (_name, minutes, key, hours, rest) => {
    expect(estimateTextOf(minutes)).toEqual({ key, params: { hours, minutes: rest } });
  });
});

describe('previewWidthOf', () => {
  it('previewWidthOf_ForSixHoursOnAThousandPixelAxis_IsAQuarterOfTheAxis', () => {
    expect(previewWidthOf(360, AXIS, 1000)).toBeCloseTo(250);
  });

  it('previewWidthOf_ForAVeryShortEstimate_KeepsAMinimumWidth', () => {
    expect(previewWidthOf(5, AXIS, 100)).toBe(8);
  });
});

describe('chipTooltipOf', () => {
  it('chipTooltipOf_ForATitleAndAnEstimateText_GivesTheTitleThenTheEstimateOnTwoLines', () => {
    const lines = chipTooltipOf('Replace filter', '1 h 30 min').split('\n');

    expect(lines).toEqual(['Replace filter', '1 h 30 min']);
  });
});
