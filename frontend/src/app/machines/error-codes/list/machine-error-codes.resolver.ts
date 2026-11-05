import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { RedirectCommand, type ResolveFn, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { catchError, EMPTY, of, throwError } from 'rxjs';

import { httpErrorMessageKey } from '@shared/http/http-error-message';

import type { Machine } from '@machines/machine';
import { MachineErrorCodesGridStore } from '@machines/error-codes/list/machine-error-codes-grid.store';

export const MACHINE_ID_PARAMETER = 'machineId';
export const NOT_FOUND_URL = '/not-found';

const ERROR_SUMMARY_KEY = 'ERRORS.SUMMARY';

/**
 * Opens the machine of the route in the grid store before the page activates. A missing machine (`404`) leads to the
 * not-found page and keeps the address bar. Another HTTP failure shows its status toast here, because the load skips
 * the interceptor toast, and completes without a value, so the router cancels the navigation and the user stays on the
 * previous page. A failure that is not an HTTP response goes to the global error handler.
 */
export const machineErrorCodesResolver: ResolveFn<Machine | RedirectCommand> = (route) => {
  const store = inject(MachineErrorCodesGridStore);
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
