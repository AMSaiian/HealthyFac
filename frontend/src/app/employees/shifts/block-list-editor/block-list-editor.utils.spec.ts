import { Temporal } from '@shared/time/temporal';

import { blockStateOf, nextBlock, nowOnGrid } from './block-list-editor.utils';

const ZONE = 'Europe/Kyiv';
const TODAY = Temporal.PlainDate.from('2027-04-12');

function at(time: string): Temporal.Instant {
  return TODAY.toPlainDateTime(Temporal.PlainTime.from(time)).toZonedDateTime(ZONE).toInstant();
}

describe('nextBlock', { tags: ['Shifts'] }, () => {
  it('nextBlock_WithoutBlocks_GivesTheFirstBlock', () => {
    const block = nextBlock([]);

    expect(block).toEqual({ start: '08:00', end: '12:00' });
  });

  it.each([
    ['AfterABlock_StartsAtItsEndAndRunsOneHour', '12:00', { start: '12:00', end: '13:00' }],
    ['NearTheDayEnd_StopsAt2355', '23:30', { start: '23:30', end: '23:55' }],
  ])('nextBlock_%s', (_name, end, expected) => {
    const block = nextBlock([{ start: '08:00', end }]);

    expect(block).toEqual(expected);
  });

  it('nextBlock_AfterABlockEndingAt2355_GivesNothing', () => {
    const block = nextBlock([{ start: '08:00', end: '23:55' }]);

    expect(block).toBeNull();
  });
});

describe('nextBlock with an earliest time', { tags: ['Shifts'] }, () => {
  it.each([
    ['ForTodayWithoutBlocks_StartsAtTheEarliestTime', [], '14:05', { start: '14:05', end: '18:05' }],
    [
      'AfterAnEndedBlock_StartsAtTheEarliestTime',
      [{ start: '08:00', end: '12:00' }],
      '13:05',
      { start: '13:05', end: '14:05' },
    ],
  ])('nextBlock_%s', (_name, blocks, earliest, expected) => {
    expect(nextBlock(blocks, earliest)).toEqual(expected);
  });

  it('nextBlock_WhenTheEarliestTimeIsTheLatestEnd_GivesNothing', () => {
    expect(nextBlock([], '23:55')).toBeNull();
  });
});

describe('blockStateOf', { tags: ['Shifts'] }, () => {
  const tomorrow = TODAY.add({ days: 1 });

  it.each([
    ['ForABlockThatEndedBeforeNow_IsEnded', { start: '08:00', end: '12:00' }, TODAY, 'ended'],
    ['ForABlockThatEndsExactlyNow_IsEnded', { start: '08:00', end: '13:02' }, TODAY, 'ended'],
    ['ForABlockAroundNow_IsRunning', { start: '13:00', end: '17:00' }, TODAY, 'running'],
    ['ForABlockThatStartsExactlyNow_IsRunning', { start: '13:02', end: '14:00' }, TODAY, 'running'],
    ['ForABlockAfterNow_IsFuture', { start: '14:00', end: '15:00' }, TODAY, 'future'],
    ['ForTomorrow_IsFuture', { start: '08:00', end: '12:00' }, tomorrow, 'future'],
    ['WithoutADate_IsFuture', { start: '08:00', end: '12:00' }, null, 'future'],
  ] as const)('blockStateOf_%s', (_name, block, date, expected) => {
    expect(blockStateOf({ ...block }, date, at('13:02'), ZONE)).toBe(expected);
  });
});

describe('nowOnGrid', { tags: ['Shifts'] }, () => {
  it.each([
    ['At1302_Gives1305', '13:02', '13:05'],
    ['At1305_Gives1305', '13:05', '13:05'],
    ['At1305AndASecond_Gives1310', '13:05:01', '13:10'],
    ['After2355_Gives2355', '23:58', '23:55'],
  ])('nowOnGrid_%s', (_name, time, expected) => {
    expect(nowOnGrid(at(time), ZONE).toString({ smallestUnit: 'minute' })).toBe(expected);
  });
});
