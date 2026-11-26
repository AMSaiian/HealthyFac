import { Temporal } from '@shared/time/temporal';

import { createOverrideFormSchema, type OverrideTodayContext } from './override-dialog-schema';

describe('createOverrideFormSchema', { tags: ['Shifts'] }, () => {
  const TODAY = Temporal.PlainDate.from('2027-04-12');
  const CONTEXT: OverrideTodayContext = {
    now: TODAY.toPlainDateTime('13:02').toZonedDateTime('Europe/Kyiv').toInstant(),
    timeZoneId: 'Europe/Kyiv',
    storedBlocks: [{ start: '08:00', end: '12:00' }],
  };

  function validate(date: Temporal.PlainDate, blocks: { start: string; end: string }[]): readonly string[] {
    const result = createOverrideFormSchema(() => CONTEXT)['~standard'].validate({ date, blocks });
    return 'issues' in (result as object)
      ? ((result as { issues: { message: string }[] }).issues ?? []).map((issue) => issue.message)
      : [];
  }

  it('validate_ForTodayWithANewBlockBeforeNow_ReportsStartBeforeNow', () => {
    expect(validate(TODAY, [{ start: '13:00', end: '14:00' }])).toContain('SHIFTS.ERRORS.START_BEFORE_NOW');
  });

  it('validate_ForTodayWithAStoredPastBlockAndANewBlockFromNowOnTheGrid_IsValid', () => {
    expect(
      validate(TODAY, [
        { start: '08:00', end: '12:00' },
        { start: '13:05', end: '14:00' },
      ]),
    ).toEqual([]);
  });

  it('validate_ForTomorrowWithAMorningBlock_IsValid', () => {
    expect(validate(TODAY.add({ days: 1 }), [{ start: '06:00', end: '07:00' }])).toEqual([]);
  });
});
