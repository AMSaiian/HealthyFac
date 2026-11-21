import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { RedirectCommand, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { catchError, EMPTY, type Observable, of, type OperatorFunction, throwError } from 'rxjs';

import { FallbackRedirects } from '@shared/http/fallback-redirects';
import { NETWORK_ERROR_URL, NOT_FOUND_URL } from '@shared/http/fallback-urls';
import { httpErrorMessageKey } from '@shared/http/http-error-message';

const ERROR_SUMMARY_KEY = 'TOASTS.ERROR';
const NETWORK_STATUS = 0;

/**
 * Handles the failure of a resolver request. Call it in the injection context of the resolver. A `404` leads to the
 * not-found page, and a network failure (status `0`) to the network page. Both keep the requested address in the
 * address bar (`browserUrl`), so a reload or Retry asks for the same page. Another HTTP failure shows its status
 * toast and completes without a value, so the router cancels the navigation. A failure that is not an HTTP response
 * goes to the global error handler.
 */
export function catchResolverFailure<T>(): OperatorFunction<T, T | RedirectCommand> {
  const router = inject(Router);
  const messages = inject(MessageService);
  const translate = inject(TranslateService);
  const redirects = inject(FallbackRedirects);

  const redirectTo = (url: string): Observable<RedirectCommand> => {
    redirects.isRedirected = true;
    return of(new RedirectCommand(router.parseUrl(url), { browserUrl: router.currentNavigation()?.finalUrl }));
  };

  return catchError((error: unknown): Observable<RedirectCommand> => {
    if (!(error instanceof HttpErrorResponse)) {
      return throwError(() => error);
    }
    if (error.status === HttpStatusCode.NotFound) {
      return redirectTo(NOT_FOUND_URL);
    }
    if (error.status === NETWORK_STATUS) {
      return redirectTo(NETWORK_ERROR_URL);
    }
    messages.add({
      severity: 'error',
      summary: translate.instant(ERROR_SUMMARY_KEY),
      detail: translate.instant(httpErrorMessageKey(error.status)),
    });
    return EMPTY;
  });
}
