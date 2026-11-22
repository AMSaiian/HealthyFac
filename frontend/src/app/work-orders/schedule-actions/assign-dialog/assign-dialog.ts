import { Component, input, output } from '@angular/core';

import type { LookupOption, LookupRequest } from '@shared/lookups/lookup-options';
import type { Temporal } from '@shared/time/temporal';

import {
  AssignmentPicker,
  type DaysRequest,
  type SpansRequest,
} from '@work-orders/assignment-picker/assignment-picker';
import type { AssignmentValue } from '@work-orders/assignment-picker/assignment-picker.utils';
import type { FreeSpan, ScheduleDay } from '@work-orders/work-order';

/**
 * The person and the start of an assign or a move. The estimate of the work order is fixed here. Both values are
 * required: the dialog reports its value as valid only when the person and the start are set.
 */
@Component({
  imports: [AssignmentPicker],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-assign-dialog',
  templateUrl: './assign-dialog.html',
})
export class AssignDialog {
  readonly initialValue = input.required<AssignmentValue>();
  readonly estimateMinutes = input.required<number | null>();
  readonly employees = input.required<LookupOption[]>();
  readonly isEmployeesLoading = input(false);
  readonly days = input<readonly ScheduleDay[] | null>(null);
  readonly isDaysLoading = input(false);
  readonly spans = input<readonly FreeSpan[] | null>(null);
  readonly isSpansLoading = input(false);
  readonly today = input.required<Temporal.PlainDate>();

  readonly valueChange = output<AssignmentValue>();
  readonly validityChange = output<boolean>();
  readonly employeeLookupRequested = output<LookupRequest>();
  readonly daysRequested = output<DaysRequest>();
  readonly spansRequested = output<SpansRequest>();

  protected onValue(value: AssignmentValue): void {
    this.valueChange.emit(value);
    this.validityChange.emit(value.assigneeId !== null && value.start !== null);
  }
}
