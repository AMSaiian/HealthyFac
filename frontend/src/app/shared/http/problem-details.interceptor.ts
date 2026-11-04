import { HttpContextToken, HttpErrorResponse, type HttpInterceptorFn, HttpStatusCode } from '@angular/common/http';
import { inject, Injector } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { catchError, throwError } from 'rxjs';

import { httpErrorMessageKey } from './http-error-message';

export const SKIP_ERROR_TOAST = new HttpContextToken<boolean>(() => false);

function hasFieldErrors(error: HttpErrorResponse): boolean {
  const body: unknown = error.error;
  return error.status === HttpStatusCode.BadRequest && typeof body === 'object' && body !== null && 'errors' in body;
}

export const problemDetailsInterceptor: HttpInterceptorFn = (request, next) => {
  const messages = inject(MessageService);
  const injector = inject(Injector);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status !== HttpStatusCode.Unauthorized &&
        !request.context.get(SKIP_ERROR_TOAST) &&
        !hasFieldErrors(error)
      ) {
        const translate = injector.get(TranslateService);
        messages.add({
          severity: 'error',
          summary: translate.instant('ERRORS.SUMMARY'),
          detail: translate.instant(httpErrorMessageKey(error.status)),
        });
      }
      return throwError(() => error);
    }),
  );
};
