import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { ANONYMOUS_REQUEST } from '@shared/http/anonymous-request';
import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';
import type { EmployeeRole } from '@shared/employee/employee';

const INVITES_URL = '/api/core/v1/employee-invites';
const VERIFY_URL = `${INVITES_URL}/verify`;
const ACCEPT_URL = `${INVITES_URL}/accept`;

export interface InviteDetails {
  readonly email: string;
  readonly companyName: string;
  readonly roleCode: EmployeeRole;
  readonly firstName: string | null;
  readonly lastName: string | null;
  readonly expiresAt: string;
}

export interface AcceptBody {
  readonly firstName: string;
  readonly lastName: string;
}

const noToast = (): { context: HttpContext } => ({
  context: new HttpContext().set(SKIP_ERROR_TOAST, true).set(ANONYMOUS_REQUEST, true),
});

/** Owns the two public calls of the invite page. The token goes only in the body, never in a URL. */
@Injectable()
export class InviteApi {
  private readonly _http = inject(HttpClient);

  verify(token: string): Observable<InviteDetails> {
    return this._http.post<InviteDetails>(VERIFY_URL, { token }, noToast());
  }

  accept(token: string, body: AcceptBody): Observable<unknown> {
    return this._http.post<unknown>(ACCEPT_URL, { token, ...body }, noToast());
  }
}
