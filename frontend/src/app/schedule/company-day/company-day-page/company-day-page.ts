import { Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';

import { ScheduleBoard } from '@shared/schedule/schedule-board/schedule-board';
import { CLOCK } from '@shared/time/clock';
import { PlainDatePicker } from '@shared/time/pickers/plain-date-picker/plain-date-picker';
import type { Temporal } from '@shared/time/temporal';

import { CompanyDayStore } from '@schedule/company-day/company-day.store';

@Component({
  imports: [ButtonDirective, PlainDatePicker, ScheduleBoard, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4' },
  selector: 'hf-company-day-page',
  templateUrl: './company-day-page.html',
})
export class CompanyDayPage {
  protected readonly store = inject(CompanyDayStore);
  protected readonly clock = inject(CLOCK);

  protected onDate(date: Temporal.PlainDate | null): void {
    if (date !== null) {
      this.store.setDate(date);
    }
  }
}
