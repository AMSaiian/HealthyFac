import { Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tooltip } from 'primeng/tooltip';

import { PlainTimePicker } from '@shared/time/pickers/plain-time-picker/plain-time-picker';
import { Temporal } from '@shared/time/temporal';

import type { ShiftBlock } from '@employees/shifts/shift';
import {
  type BlockState,
  blockStateOf,
  nextBlock,
  nowOnGrid,
} from '@employees/shifts/block-list-editor/block-list-editor.utils';
import { BLOCK_GRID_MINUTES, BLOCK_LATEST_END } from '@employees/shifts/shift-limits';

interface BlockRow {
  readonly start: Temporal.PlainTime;
  readonly end: Temporal.PlainTime;
  readonly state: BlockState;
  readonly minStart: Temporal.PlainTime | undefined;
  readonly minEnd: Temporal.PlainTime | undefined;
}

/**
 * Edits the work blocks of one day: the times are 5-minute steps and end at 23:55 at the latest. For today (the
 * inputs `date`, `now`, and `timeZoneId`), a stored block (a block of `storedBlocks` with the same start) that ended is
 * read-only, and a stored block that runs keeps its start. Every other block is a new block: it stays editable, and its
 * start is not before now on the grid.
 */
@Component({
  imports: [ButtonDirective, PlainTimePicker, Tooltip, TranslatePipe],
  host: { class: 'flex flex-col gap-1' },
  selector: 'hf-block-list-editor',
  templateUrl: './block-list-editor.html',
})
export class BlockListEditor {
  readonly blocks = input.required<readonly ShiftBlock[]>();
  readonly label = input.required<string>();
  readonly isReadOnly = input(false);
  /** The blocks that the day already has. Only they can be `ended` or `running`. */
  readonly storedBlocks = input<readonly ShiftBlock[]>([]);
  readonly date = input<Temporal.PlainDate | null>(null);
  readonly now = input<Temporal.Instant | null>(null);
  readonly timeZoneId = input<string | null>(null);

  readonly blocksChange = output<readonly ShiftBlock[]>();

  protected readonly stepMinute = BLOCK_GRID_MINUTES;
  protected readonly latestEnd = Temporal.PlainTime.from(BLOCK_LATEST_END);

  /** Now on the grid when the date is today, else `undefined`: no minimum applies. */
  protected readonly earliest = computed((): Temporal.PlainTime | undefined => {
    const now = this.now();
    const timeZoneId = this.timeZoneId();
    const date = this.date();
    const isToday =
      now !== null &&
      timeZoneId !== null &&
      date !== null &&
      date.equals(now.toZonedDateTimeISO(timeZoneId).toPlainDate());
    return isToday ? nowOnGrid(now, timeZoneId) : undefined;
  });
  protected readonly rows = computed((): BlockRow[] => {
    const now = this.now();
    const timeZoneId = this.timeZoneId();
    const earliest = this.earliest();
    return this.blocks().map((block) => {
      const isStored = this.storedBlocks().some((stored) => stored.start === block.start);
      const state: BlockState =
        now === null || timeZoneId === null || !isStored ? 'future' : blockStateOf(block, this.date(), now, timeZoneId);
      return {
        start: Temporal.PlainTime.from(block.start),
        end: Temporal.PlainTime.from(block.end),
        state,
        minStart: state === 'future' ? earliest : undefined,
        minEnd: state === 'running' ? earliest : undefined,
      };
    });
  });
  private readonly _next = computed(() =>
    nextBlock(this.blocks(), this.earliest()?.toString({ smallestUnit: 'minute' })),
  );
  protected readonly canAdd = computed(() => this._next() !== null);

  protected add(): void {
    const next = this._next();
    if (next !== null) {
      this.blocksChange.emit([...this.blocks(), next]);
    }
  }

  protected remove(index: number): void {
    this.blocksChange.emit(this.blocks().filter((_, position) => position !== index));
  }

  protected change(index: number, part: 'start' | 'end', time: Temporal.PlainTime | null): void {
    const current = this.blocks()[index];
    if (time === null || current === undefined) {
      return;
    }
    const value = time.toString({ smallestUnit: 'minute' });
    if (current[part] === value) {
      return;
    }
    this.blocksChange.emit(
      this.blocks().map((block, position) => (position === index ? { ...block, [part]: value } : block)),
    );
  }
}
