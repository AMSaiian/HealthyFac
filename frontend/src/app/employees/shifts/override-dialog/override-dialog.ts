import { Component, computed, effect, input, linkedSignal, output } from '@angular/core';
import { form, validateStandardSchema } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';

import { delayedLoading } from '@shared/loading/delayed-loading';
import {
  type PickerCellContext,
  PlainDatePicker,
  type ShownMonth,
} from '@shared/time/pickers/plain-date-picker/plain-date-picker';
import type { Temporal } from '@shared/time/temporal';

import { BlockListEditor } from '@employees/shifts/block-list-editor/block-list-editor';
import {
  createOverrideFormSchema,
  type OverrideFormValue,
} from '@employees/shifts/override-dialog/override-dialog-schema';
import type { ShiftBlock } from '@employees/shifts/shift';
import { disabledOverrideDatesOf } from '@employees/shifts/shifts-store/shifts.store.utils';
import { Divider } from 'primeng/divider';
import { Skeleton } from 'primeng/skeleton';
import { Tooltip } from 'primeng/tooltip';

/** The form of an override: one date and its blocks. No block means a day off. */
@Component({
  imports: [Divider, BlockListEditor, PlainDatePicker, Skeleton, Tooltip, TranslatePipe],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-override-dialog',
  templateUrl: './override-dialog.html',
})
export class OverrideDialog {
  readonly initialValue = input.required<OverrideFormValue>();
  readonly isEdit = input(false);
  readonly minDate = input.required<Temporal.PlainDate>();
  readonly now = input.required<Temporal.Instant>();
  readonly timeZoneId = input.required<string>();
  /** The blocks of the chosen date for a new override, or `null` while they load. */
  readonly loadedBlocks = input<readonly ShiftBlock[] | null>(null);
  readonly isLoading = input(false);
  /** Whether the chosen date of a new override already has an override. */
  readonly isDateTaken = input(false);
  /** The days of the shown month that already have an override. A new override cannot pick them. */
  readonly takenDates = input<readonly Temporal.PlainDate[]>([]);

  readonly valueChange = output<OverrideFormValue>();
  readonly monthChange = output<ShownMonth>();
  readonly validityChange = output<boolean>();

  protected readonly model = linkedSignal<OverrideFormValue>(() => ({
    date: this.initialValue().date,
    blocks: [...this.initialValue().blocks],
  }));
  protected readonly showSkeleton = delayedLoading(this.isLoading);
  protected readonly disabledDates = computed(() => disabledOverrideDatesOf(this.isEdit(), this.takenDates()));
  protected readonly storedBlocks = computed((): readonly ShiftBlock[] =>
    this.isEdit() ? this.initialValue().blocks : (this.loadedBlocks() ?? []),
  );
  protected readonly overrideForm = form(this.model, (path) => {
    validateStandardSchema(
      path,
      createOverrideFormSchema(() => ({
        now: this.now(),
        timeZoneId: this.timeZoneId(),
        storedBlocks: this.storedBlocks(),
      })),
    );
  });
  protected readonly messages = computed(() => [
    ...new Set(
      this.overrideForm()
        .errorSummary()
        .map((error) => error.message ?? ''),
    ),
  ]);

  private _isEdited = false;

  constructor() {
    effect(() => {
      const loaded = this.loadedBlocks();
      if (loaded !== null && !this._isEdited) {
        this.model.update((value) => ({ ...value, blocks: loaded.map((block) => ({ ...block })) }));
      }
    });
    effect(() => {
      this.valueChange.emit(this.model());
    });
    effect(() => {
      this.validityChange.emit(this.overrideForm().valid());
    });
  }

  protected isTaken(cell: PickerCellContext): boolean {
    return this.disabledDates().some(
      (date) => date.year === cell.year && date.month === cell.month + 1 && date.day === cell.day,
    );
  }

  protected setDate(date: Temporal.PlainDate | null): void {
    this.model.update((value) => ({ ...value, date }));
  }

  protected setBlocks(blocks: readonly ShiftBlock[]): void {
    this._isEdited = true;
    this.model.update((value) => ({ ...value, blocks: [...blocks] }));
  }
}
