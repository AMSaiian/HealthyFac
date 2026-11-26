import {
  END_AFTER_START_ERROR_KEY,
  GRID_ERROR_KEY,
  LATEST_END_ERROR_KEY,
  OVERLAP_ERROR_KEY,
  shiftBlockListSchema,
  UNSORTED_ERROR_KEY,
} from './shift-blocks-schema';

function messagesOf(blocks: unknown): string[] {
  const result = shiftBlockListSchema.safeParse(blocks);
  return result.success ? [] : result.error.issues.map((issue) => issue.message);
}

const block = (start: string, end: string): { start: string; end: string } => ({ start, end });

describe('shiftBlockListSchema', { tags: ['Shifts'] }, () => {
  it('shiftBlockListSchema_WithTwoValidBlocks_Passes', () => {
    const result = shiftBlockListSchema.safeParse([block('08:00', '12:00'), block('13:00', '23:55')]);

    expect(result.success).toBe(true);
  });

  it('shiftBlockListSchema_WithNoBlocks_PassesAsADayOff', () => {
    const result = shiftBlockListSchema.safeParse([]);

    expect(result.success).toBe(true);
  });

  it.each(
    [
      { name: 'WithEnd2355', blocks: [block('16:00', '23:55')] },
      { name: 'WithBlocksThatTouch', blocks: [block('08:00', '12:00'), block('12:00', '13:00')] },
      { name: 'WithSixBlocks', blocks: Array.from({ length: 6 }, (_, index) => block(`0${index}:00`, `0${index}:30`)) },
    ].map((row) => [row.name, row] as [string, typeof row]),
  )('shiftBlockListSchema_%s_Accepts', (_name, { blocks }) => {
    expect(messagesOf(blocks)).toEqual([]);
  });

  it.each([
    ['WithEnd2400_RejectsWithTheLatestEndKey', [block('16:00', '24:00')], LATEST_END_ERROR_KEY],
    ['WithTimeOffTheGrid_RejectsWithTheGridKey', [block('08:03', '12:00')], GRID_ERROR_KEY],
    ['WithEndNotAfterStart_RejectsWithTheEndKey', [block('12:00', '12:00')], END_AFTER_START_ERROR_KEY],
    [
      'WithUnsortedBlocks_RejectsWithTheSortKey',
      [block('13:00', '14:00'), block('08:00', '12:00')],
      UNSORTED_ERROR_KEY,
    ],
    [
      'WithOverlappingBlocks_RejectsWithTheOverlapKey',
      [block('08:00', '12:00'), block('11:00', '13:00')],
      OVERLAP_ERROR_KEY,
    ],
  ])('shiftBlockListSchema_%s', (_name, blocks, key) => {
    expect(messagesOf(blocks)).toContain(key);
  });
});
