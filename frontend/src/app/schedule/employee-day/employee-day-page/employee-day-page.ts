import { Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tooltip } from 'primeng/tooltip';

import { ScheduleBoard } from '@shared/schedule/schedule-board/schedule-board';
import { CLOCK } from '@shared/time/clock';
import { PlainDatePicker } from '@shared/time/pickers/plain-date-picker/plain-date-picker';
import type { Temporal } from '@shared/time/temporal';
import { HistoryStore } from '@shared/history/history.store';

import { EmployeeDayStore } from '@schedule/employee-day/employee-day.store';
import { EMPLOYEES_RETURN_TARGET } from '@employees/employees-return-target';

@Component({
  imports: [ButtonDirective, PlainDatePicker, ScheduleBoard, Tooltip, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4' },
  selector: 'hf-employee-day-page',
  templateUrl: './employee-day-page.html',
})
export class EmployeeDayPage {
  protected readonly store = inject(EmployeeDayStore);
  protected readonly history = inject(HistoryStore);
  protected readonly returnTarget = EMPLOYEES_RETURN_TARGET;
  protected readonly clock = inject(CLOCK);

  protected onDate(date: Temporal.PlainDate | null): void {
    if (date !== null) {
      this.store.setDate(date);
    }
  }
}
