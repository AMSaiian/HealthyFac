import { Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tooltip } from 'primeng/tooltip';

import { PlainTimePicker } from '@shared/time/pickers/plain-time-picker/plain-time-picker';
import { Temporal } from '@shared/time/temporal';

import type { ShiftBlock } from '@employees/shifts/shift';
import { nextBlock } from '@employees/shifts/block-list-editor/block-list-editor.utils';
import { BLOCK_GRID_MINUTES, BLOCK_LATEST_END, MAX_BLOCKS_PER_DAY } from '@employees/shifts/shift-limits';

/** Edits the work blocks of one day: the times are 5-minute steps and end at 23:55 at the latest. */
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

  readonly blocksChange = output<readonly ShiftBlock[]>();

  protected readonly stepMinute = BLOCK_GRID_MINUTES;
  protected readonly latestEnd = Temporal.PlainTime.from(BLOCK_LATEST_END);
  protected readonly maxBlocks = MAX_BLOCKS_PER_DAY;

  protected readonly rows = computed(() =>
    this.blocks().map((block) => ({
      start: Temporal.PlainTime.from(block.start),
      end: Temporal.PlainTime.from(block.end),
    })),
  );
  protected readonly canAdd = computed(
    () => this.blocks().length < this.maxBlocks && nextBlock(this.blocks()) !== null,
  );

  protected add(): void {
    const next = nextBlock(this.blocks());
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
