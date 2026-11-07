import { inject } from '@angular/core';
import type { CanDeactivateFn } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService } from 'primeng/api';
import { Observable } from 'rxjs';

import { MetricsPageStore } from '@machines/metrics/metrics-page.store';

/** Asks before an in-app navigation leaves the page with staged changes or a changed open form. */
export const metricsPageGuard: CanDeactivateFn<unknown> = () => {
  const store = inject(MetricsPageStore);
  const confirmation = inject(ConfirmationService);
  const translate = inject(TranslateService);

  if (!store.hasPendingWork()) {
    return true;
  }
  return new Observable<boolean>((subscriber) => {
    confirmation.confirm({
      header: translate.instant('METRICS.CONFIRM.HEADER'),
      message: translate.instant('METRICS.CONFIRM.LEAVE_MESSAGE'),
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: translate.instant('METRICS.CONFIRM.ACCEPT'),
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
