import { Component, effect, inject, input } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';

import { ScheduleBoard } from '@shared/schedule/schedule-board/schedule-board';
import { CLOCK } from '@shared/time/clock';
import { PlainDatePicker } from '@shared/time/pickers/plain-date-picker/plain-date-picker';
import type { Temporal } from '@shared/time/temporal';

import { EmployeeDayStore } from '@schedule/employee-day/employee-day.store';

const NOT_FOUND_URL = '/not-found';

@Component({
  imports: [ButtonDirective, PlainDatePicker, RouterLink, ScheduleBoard, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4' },
  selector: 'hf-employee-day-page',
  templateUrl: './employee-day-page.html',
})
export class EmployeeDayPage {
  readonly id = input.required<string>();

  protected readonly store = inject(EmployeeDayStore);
  protected readonly clock = inject(CLOCK);

  private readonly _router = inject(Router);

  constructor() {
    effect(() => {
      this.store.setEmployee(this.id());
    });
    effect(() => {
      if (this.store.isNotFound()) {
        void this._router.navigateByUrl(NOT_FOUND_URL, { skipLocationChange: true });
      }
    });
  }

  protected onDate(date: Temporal.PlainDate | null): void {
    if (date !== null) {
      this.store.setDate(date);
    }
  }
}
