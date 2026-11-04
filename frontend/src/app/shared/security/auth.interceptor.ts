import { HttpErrorResponse, type HttpEvent, type HttpInterceptorFn, HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, defer, from, type Observable, of, switchMap, throwError } from 'rxjs';

import { APP_CONFIG } from '@shared/config/app-config';

import { AuthStore } from './auth.store';
import { SessionEndedError } from './session-errors';

const isUnauthorized = (error: unknown): boolean =>
  error instanceof HttpErrorResponse && error.status === HttpStatusCode.Unauthorized;

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const config = inject(APP_CONFIG);
  if (!request.url.startsWith(`${config.apiBaseUrl}/`)) {
    return next(request);
  }
  const store = inject(AuthStore);
  const router = inject(Router);

  const loginAndRethrow = (error: unknown): Observable<never> => {
    void store.login(router.url);
    return throwError(() => error);
  };
  const handleSessionFailure = (error: unknown): Observable<never> =>
    error instanceof SessionEndedError
      ? loginAndRethrow(error)
      : throwError(() => new HttpErrorResponse({ status: 0, url: request.url, error }));
  let sentToken: string | undefined;
  const send = (token: string): Observable<HttpEvent<unknown>> => {
    sentToken = token;
    return next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
  };
  const refreshUnlessNewerTokenExists = (): Observable<void> => {
    const current = store.currentToken();
    return current !== undefined && current !== sentToken ? of(undefined) : defer(() => from(store.forceRefresh()));
  };

  return defer(() => from(store.accessToken())).pipe(
    catchError(handleSessionFailure),
    switchMap(send),
    catchError((error: unknown) => {
      if (!isUnauthorized(error)) {
        return throwError(() => error);
      }
      return refreshUnlessNewerTokenExists().pipe(
        switchMap(() => from(store.accessToken())),
        catchError(handleSessionFailure),
        switchMap(send),
        catchError((retryError: unknown) =>
          isUnauthorized(retryError) ? loginAndRethrow(retryError) : throwError(() => retryError),
        ),
      );
    }),
  );
};
