import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { RedirectCommand, type ResolveFn, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { catchError, EMPTY, of, throwError } from 'rxjs';

import { httpErrorMessageKey } from '@shared/http/http-error-message';

import type { CaseDetail } from '@maintenance/case';
import { CasePageStore } from '@maintenance/detail/case-page-store/case-page.store';

export const CASE_ID_PARAMETER = 'caseId';
const NOT_FOUND_URL = '/not-found';
const ERROR_SUMMARY_KEY = 'TOASTS.ERROR';

/**
 * Opens the case of the route in the case store before the page activates. A missing case (`404`) leads to the
 * not-found page. Another HTTP failure shows its status toast and cancels the navigation.
 */
export const caseResolver: ResolveFn<CaseDetail | RedirectCommand> = (route) => {
  const store = inject(CasePageStore);
  const router = inject(Router);
  const messages = inject(MessageService);
  const translate = inject(TranslateService);

  return store.openCase(route.paramMap.get(CASE_ID_PARAMETER) ?? '').pipe(
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
