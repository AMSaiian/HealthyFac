import { Temporal } from '@shared/time/temporal';

import { blockStateOf, nextBlock, nowOnGrid } from './block-list-editor.utils';

const ZONE = 'Europe/Kyiv';
const TODAY = Temporal.PlainDate.from('2027-04-12');

function at(time: string): Temporal.Instant {
  return TODAY.toPlainDateTime(Temporal.PlainTime.from(time)).toZonedDateTime(ZONE).toInstant();
}

describe('nextBlock', () => {
  it('nextBlock_WithoutBlocks_GivesTheFirstBlock', () => {
    const block = nextBlock([]);

    expect(block).toEqual({ start: '08:00', end: '12:00' });
  });

  it('nextBlock_AfterABlock_StartsAtItsEndAndRunsOneHour', () => {
    const block = nextBlock([{ start: '08:00', end: '12:00' }]);

    expect(block).toEqual({ start: '12:00', end: '13:00' });
  });

  it('nextBlock_NearTheDayEnd_StopsAt2355', () => {
    const block = nextBlock([{ start: '08:00', end: '23:30' }]);

    expect(block).toEqual({ start: '23:30', end: '23:55' });
  });

  it('nextBlock_AfterABlockEndingAt2355_GivesNothing', () => {
    const block = nextBlock([{ start: '08:00', end: '23:55' }]);

    expect(block).toBeNull();
  });
});

describe('nextBlock with an earliest time', () => {
  it('nextBlock_ForTodayWithoutBlocks_StartsAtTheEarliestTime', () => {
    expect(nextBlock([], '14:05')).toEqual({ start: '14:05', end: '18:05' });
  });

  it('nextBlock_AfterAnEndedBlock_StartsAtTheEarliestTime', () => {
    expect(nextBlock([{ start: '08:00', end: '12:00' }], '13:05')).toEqual({ start: '13:05', end: '14:05' });
  });

  it('nextBlock_WhenTheEarliestTimeIsTheLatestEnd_GivesNothing', () => {
    expect(nextBlock([], '23:55')).toBeNull();
  });
});

describe('blockStateOf', () => {
  it('blockStateOf_ForABlockThatEndedBeforeNow_IsEnded', () => {
    expect(blockStateOf({ start: '08:00', end: '12:00' }, TODAY, at('13:02'), ZONE)).toBe('ended');
  });

  it('blockStateOf_ForABlockThatEndsExactlyNow_IsEnded', () => {
    expect(blockStateOf({ start: '08:00', end: '13:02' }, TODAY, at('13:02'), ZONE)).toBe('ended');
  });

  it('blockStateOf_ForABlockAroundNow_IsRunning', () => {
    expect(blockStateOf({ start: '13:00', end: '17:00' }, TODAY, at('13:02'), ZONE)).toBe('running');
  });

  it('blockStateOf_ForABlockThatStartsExactlyNow_IsRunning', () => {
    expect(blockStateOf({ start: '13:02', end: '14:00' }, TODAY, at('13:02'), ZONE)).toBe('running');
  });

  it('blockStateOf_ForABlockAfterNow_IsFuture', () => {
    expect(blockStateOf({ start: '14:00', end: '15:00' }, TODAY, at('13:02'), ZONE)).toBe('future');
  });

  it('blockStateOf_ForTomorrow_IsFuture', () => {
    expect(blockStateOf({ start: '08:00', end: '12:00' }, TODAY.add({ days: 1 }), at('13:02'), ZONE)).toBe('future');
  });

  it('blockStateOf_WithoutADate_IsFuture', () => {
    expect(blockStateOf({ start: '08:00', end: '12:00' }, null, at('13:02'), ZONE)).toBe('future');
  });
});

describe('nowOnGrid', () => {
  it('nowOnGrid_At1302_Gives1305', () => {
    expect(nowOnGrid(at('13:02'), ZONE).toString({ smallestUnit: 'minute' })).toBe('13:05');
  });

  it('nowOnGrid_At1305_Gives1305', () => {
    expect(nowOnGrid(at('13:05'), ZONE).toString({ smallestUnit: 'minute' })).toBe('13:05');
  });

  it('nowOnGrid_At1305AndASecond_Gives1310', () => {
    expect(nowOnGrid(at('13:05:01'), ZONE).toString({ smallestUnit: 'minute' })).toBe('13:10');
  });

  it('nowOnGrid_After2355_Gives2355', () => {
    expect(nowOnGrid(at('23:58'), ZONE).toString({ smallestUnit: 'minute' })).toBe('23:55');
  });
});
