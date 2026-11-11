import { Component, effect, input, linkedSignal, output } from '@angular/core';
import { type FieldTree, form, FormField, validateStandardSchema } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Select } from 'primeng/select';

import { isFieldInvalid } from '@shared/forms/field-state';
import { keepSelectedOptions, type LookupOption, type LookupRequest } from '@shared/lookups/lookup-options';
import { PlainDateTimePicker } from '@shared/time/pickers/plain-date-time-picker/plain-date-time-picker';
import type { Temporal } from '@shared/time/temporal';

import { assignFormSchema, type AssignFormValue } from './assign-dialog-schema';
import { GRID_STEP_MINUTES } from '@work-orders/schedule-actions/slot-prefill';

export interface AssignSelection {
  readonly assigneeId: string;
  readonly date: Temporal.PlainDate;
}

/**
 * Tells whether two selections name the same person and day. The store reloads the day schedule for each new
 * selection object, so an equal one must not be emitted again.
 */
function isSameSelection(first: AssignSelection | null, second: AssignSelection | null): boolean {
  if (first === null || second === null) {
    return first === second;
  }
  return first.assigneeId === second.assigneeId && first.date.equals(second.date);
}

/** The people and the start of an assign or move. The suggestion is the first free start of the chosen day. */
@Component({
  imports: [ButtonDirective, FormField, PlainDateTimePicker, Select, TranslatePipe],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-assign-dialog',
  templateUrl: './assign-dialog.html',
})
export class AssignDialog {
  readonly initialValue = input.required<AssignFormValue>();
  readonly employees = input.required<LookupOption[]>();
  readonly isEmployeesLoading = input(false);
  readonly suggestion = input<Temporal.PlainDateTime | null>(null);
  readonly hasNoFreeStart = input(false);

  readonly valueChange = output<AssignFormValue>();
  readonly validityChange = output<boolean>();
  readonly selectionChange = output<AssignSelection | null>();
  readonly employeeLookupRequested = output<LookupRequest>();

  protected readonly stepMinute = GRID_STEP_MINUTES;

  protected readonly model = linkedSignal<AssignFormValue>(() => ({ ...this.initialValue() }));
  protected readonly employeeOptions = keepSelectedOptions(this.employees, () => [this.model().assigneeId]);
  protected readonly assignForm = form(this.model, (path) => {
    validateStandardSchema(path, assignFormSchema);
  });

  private _lastApplied: Temporal.PlainDateTime | null = null;
  private _lastSelection: AssignSelection | null = null;

  constructor() {
    effect(() => {
      this.valueChange.emit(this.model());
    });
    effect(() => {
      this.validityChange.emit(this.assignForm().valid());
    });
    effect(() => {
      const { assigneeId, start } = this.model();
      const selection = assigneeId === '' || start === null ? null : { assigneeId, date: start.toPlainDate() };
      if (!isSameSelection(this._lastSelection, selection)) {
        this._lastSelection = selection;
        this.selectionChange.emit(selection);
      }
    });
    effect(() => {
      const suggestion = this.suggestion();
      const start = this.model().start;
      if (suggestion !== null && (start === null || this._lastApplied === null || start.equals(this._lastApplied))) {
        this._lastApplied = suggestion;
        if (start === null || !start.equals(suggestion)) {
          this.model.update((value) => ({ ...value, start: suggestion }));
        }
      }
    });
  }

  protected useSuggestion(): void {
    const suggestion = this.suggestion();
    if (suggestion !== null) {
      this._lastApplied = suggestion;
      this.model.update((value) => ({ ...value, start: suggestion }));
    }
  }

  protected isShown(field: FieldTree<unknown>): boolean {
    return isFieldInvalid({ isInvalid: field().invalid(), isTouched: field().touched() });
  }
}
