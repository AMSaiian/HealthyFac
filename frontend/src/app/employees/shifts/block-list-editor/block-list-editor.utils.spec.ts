import { nextBlock } from './block-list-editor.utils';

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
