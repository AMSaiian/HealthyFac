import { custom, type infer as Infer, object, refine } from 'zod/mini';

import { Temporal } from '@shared/time/temporal';

import { nowOnGrid } from '@employees/shifts/block-list-editor/block-list-editor.utils';
import type { ShiftBlock } from '@employees/shifts/shift';
import { shiftBlockListSchema } from '@employees/shifts/shift-blocks-schema';
import { DATE_REQUIRED_ERROR_KEY } from '@employees/shifts/version-dialog/version-dialog-schema';

export const START_BEFORE_NOW_ERROR_KEY = 'SHIFTS.ERRORS.START_BEFORE_NOW';

const overrideFormShape = object({
  date: custom<Temporal.PlainDate | null>((value) => value instanceof Temporal.PlainDate, DATE_REQUIRED_ERROR_KEY),
  blocks: shiftBlockListSchema,
});

/** What the rule of today needs: the clock, and the blocks that the stored override had when the dialog opened. */
export interface OverrideTodayContext {
  readonly now: Temporal.Instant;
  readonly timeZoneId: string;
  readonly storedBlocks: readonly ShiftBlock[];
}

/**
 * The override form: one date and its blocks. No block means a day off. For today, a block that starts before now on
 * the grid must be a block of the stored override with the same start, so a new block never starts in the past.
 * `context` is read at each validation, so the rule follows the signals that it reads.
 */
export function createOverrideFormSchema(context: () => OverrideTodayContext | null): typeof overrideFormShape {
  return overrideFormShape.check(
    refine((value) => {
      const today = context();
      if (today === null || value.date === null) {
        return true;
      }
      if (!value.date.equals(today.now.toZonedDateTimeISO(today.timeZoneId).toPlainDate())) {
        return true;
      }
      const earliest = nowOnGrid(today.now, today.timeZoneId).toString({ smallestUnit: 'minute' });
      return value.blocks.every(
        (block) => block.start >= earliest || today.storedBlocks.some((stored) => stored.start === block.start),
      );
    }, START_BEFORE_NOW_ERROR_KEY),
  );
}

export type OverrideFormValue = Infer<typeof overrideFormShape>;
