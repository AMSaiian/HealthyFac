import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';

import type { CurrentEmployee } from './employee';

const CURRENT_EMPLOYEE_URL = '/api/core/v1/employees/me';

/** Owns the HTTP call about the employee of the signed-in user. */
@Injectable({ providedIn: 'root' })
export class EmployeeApi {
  private readonly _http = inject(HttpClient);

  current(): Observable<CurrentEmployee> {
    return this._http.get<CurrentEmployee>(CURRENT_EMPLOYEE_URL, {
      context: new HttpContext().set(SKIP_ERROR_TOAST, true),
    });
  }
}
