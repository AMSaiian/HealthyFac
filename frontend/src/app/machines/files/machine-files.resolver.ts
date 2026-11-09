import { HttpContext, HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { RedirectCommand, type ResolveFn, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { catchError, EMPTY, of, throwError } from 'rxjs';

import { httpErrorMessageKey } from '@shared/http/http-error-message';
import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';

import type { Machine } from '@machines/machine';
import { MachinesApi } from '@machines/machines-api';

export const MACHINE_ID_PARAMETER = 'machineId';
export const NOT_FOUND_URL = '/not-found';

const ERROR_SUMMARY_KEY = 'ERRORS.SUMMARY';

/**
 * Loads the machine of the route before the files page activates. A missing machine (`404`) leads to the not-found
 * page and keeps the address bar. Another HTTP failure shows its status toast here, because the load skips the
 * interceptor toast, and completes without a value, so the router cancels the navigation. A failure that is not an
 * HTTP response goes to the global error handler.
 */
export const machineFilesResolver: ResolveFn<Machine | RedirectCommand> = (route) => {
  const api = inject(MachinesApi);
  const router = inject(Router);
  const messages = inject(MessageService);
  const translate = inject(TranslateService);

  return api
    .get(route.paramMap.get(MACHINE_ID_PARAMETER) ?? '', { context: new HttpContext().set(SKIP_ERROR_TOAST, true) })
    .pipe(
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
