import { inject } from '@angular/core';
import type { CanDeactivateFn } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService } from 'primeng/api';
import { Observable } from 'rxjs';

import type { SchedulePage } from '@work-orders/schedule/schedule-page/schedule-page';
import { ScheduleStore } from '@work-orders/schedule/schedule-store/schedule.store';

/**
 * Asks before an in-app navigation leaves the schedule tab with pending changes. A day switch inside the tab never
 * asks, because it does not leave the route.
 */
export const schedulePageGuard: CanDeactivateFn<SchedulePage> = () => {
  const store = inject(ScheduleStore);
  if (store.pendingCount() === 0) {
    return true;
  }
  const confirmation = inject(ConfirmationService);
  const translate = inject(TranslateService);
  return new Observable<boolean>((subscriber) => {
    confirmation.confirm({
      header: translate.instant('WORK_ORDERS.SCHEDULE_TAB.CONFIRM.LEAVE_HEADER'),
      message: translate.instant('WORK_ORDERS.SCHEDULE_TAB.CONFIRM.LEAVE_MESSAGE'),
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: translate.instant('WORK_ORDERS.SCHEDULE_TAB.CONFIRM.LEAVE_ACCEPT'),
      rejectLabel: translate.instant('ACTIONS.CANCEL'),
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary' },
      accept: () => {
        subscriber.next(true);
        subscriber.complete();
      },
      reject: () => {
        subscriber.next(false);
        subscriber.complete();
      },
    });
  });
};
