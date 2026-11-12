import { HttpContext, HttpErrorResponse, type HttpStatusCode } from '@angular/common/http';

import { httpErrorMessageKey } from './http-error-message';
import { SKIP_ERROR_TOAST } from './problem-details.interceptor';

const NETWORK_STATUS = 0;

export function httpErrorKey(error: unknown, overrides: Partial<Record<HttpStatusCode, string>> = {}): string {
  const status = error instanceof HttpErrorResponse ? error.status : NETWORK_STATUS;
  const override = overrides[status as HttpStatusCode];

  return override ?? httpErrorMessageKey(status);
}

/** Tells whether the error is an HTTP failure with one of the statuses. */
export function isHttpStatus(error: unknown, ...statuses: HttpStatusCode[]): boolean {
  return error instanceof HttpErrorResponse && statuses.includes(error.status);
}

export function skipErrorToast(): { context: HttpContext } {
  return { context: new HttpContext().set(SKIP_ERROR_TOAST, true) };
}
