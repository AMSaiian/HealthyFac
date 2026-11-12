import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { RedirectCommand, type ResolveFn, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { catchError, EMPTY, of, throwError } from 'rxjs';

import { httpErrorMessageKey } from '@shared/http/http-error-message';

import type { Machine } from '@machines/machine';
import { MetricsPageStore } from '@machines/metrics/metrics-page-store/metrics-page.store';
import { MACHINE_ID_PARAMETER, NOT_FOUND_URL } from '@machines/statuses/list/status-definitions.resolver';

const ERROR_SUMMARY_KEY = 'TOASTS.ERROR';

/**
 * Opens the machine of the route in the metrics store before the page activates. A missing machine (`404`) leads to the
 * not-found page. Another HTTP failure shows its status toast and cancels the navigation.
 */
export const metricsResolver: ResolveFn<Machine | RedirectCommand> = (route) => {
  const store = inject(MetricsPageStore);
  const router = inject(Router);
  const messages = inject(MessageService);
  const translate = inject(TranslateService);

  return store.openMachine(route.paramMap.get(MACHINE_ID_PARAMETER) ?? '').pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }
      if (error.status === HttpStatusCode.NotFound) {
        return of(new RedirectCommand(router.parseUrl(NOT_FOUND_URL), { skipLocationChange: true }));
      }
      messages.add({
        severity: 'error',
        summary: translate.instant(ERROR_SUMMARY_KEY),
        detail: translate.instant(httpErrorMessageKey(error.status)),
      });
      return EMPTY;
    }),
  );
};
