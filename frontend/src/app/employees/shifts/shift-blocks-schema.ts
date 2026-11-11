import { array, type infer as Infer, maxLength, object, refine, regex, string } from 'zod/mini';

import { BLOCK_GRID_MINUTES, BLOCK_LATEST_END, MAX_BLOCKS_PER_DAY } from '@employees/shifts/shift-limits';

export const TIME_FORMAT_ERROR_KEY = 'SHIFTS.ERRORS.TIME_FORMAT';
export const GRID_ERROR_KEY = 'SHIFTS.ERRORS.GRID';
export const LATEST_END_ERROR_KEY = 'SHIFTS.ERRORS.LATEST_END';
export const END_AFTER_START_ERROR_KEY = 'SHIFTS.ERRORS.END_AFTER_START';
export const UNSORTED_ERROR_KEY = 'SHIFTS.ERRORS.UNSORTED';
export const OVERLAP_ERROR_KEY = 'SHIFTS.ERRORS.OVERLAP';
export const TOO_MANY_ERROR_KEY = 'SHIFTS.ERRORS.TOO_MANY';

const TIME_PATTERN = /^\d{2}:\d{2}$/;
const MINUTES_PER_HOUR = 60;

function minutesOf(time: string): number {
  const [hours, minutes] = time.split(':').map(Number);
  return hours * MINUTES_PER_HOUR + minutes;
}

const timeText = string().check(regex(TIME_PATTERN, TIME_FORMAT_ERROR_KEY));

/**
 * One work block. The rules mirror the backend: the times lie on the 5-minute grid, the block ends at 23:55 at the
 * latest, and it ends after it starts. The messages are translation keys.
 */
export const shiftBlockSchema = object({ start: timeText, end: timeText }).check(
  refine(
    (block) => minutesOf(block.start) % BLOCK_GRID_MINUTES === 0 && minutesOf(block.end) % BLOCK_GRID_MINUTES === 0,
    GRID_ERROR_KEY,
  ),
  refine((block) => block.end <= BLOCK_LATEST_END, LATEST_END_ERROR_KEY),
  refine((block) => block.end > block.start, END_AFTER_START_ERROR_KEY),
);

/** The blocks of one day: at most 6, sorted by start, without overlap. An empty list is a day without work. */
export const shiftBlockListSchema = array(shiftBlockSchema).check(
  maxLength(MAX_BLOCKS_PER_DAY, TOO_MANY_ERROR_KEY),
  refine(
    (blocks) => blocks.every((block, index) => index === 0 || block.start >= blocks[index - 1].start),
    UNSORTED_ERROR_KEY,
  ),
  refine(
    (blocks) => blocks.every((block, index) => index === 0 || block.start >= blocks[index - 1].end),
    OVERLAP_ERROR_KEY,
  ),
);

export type ShiftBlockValue = Infer<typeof shiftBlockSchema>;
