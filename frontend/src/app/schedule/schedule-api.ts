import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';

import type { DaySchedule } from '@work-orders/work-order';

const EMPLOYEES_URL = '/api/core/v1/employees';
const COMPANY_SCHEDULE_URL = '/api/core/v1/company/schedule';
const SCHEDULE_SEGMENT = 'schedule';
const DATE_PARAMETER = 'date';

export interface CompanyDayEmployee {
  readonly employeeId: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly day: DaySchedule;
}

export interface CompanyDaySchedule {
  readonly date: string;
  readonly timeZoneId: string;
  readonly employees: readonly CompanyDayEmployee[];
}

/** Owns the HTTP calls of the schedule pages. Provide it on the schedule routes. */
@Injectable()
export class ScheduleApi {
  private readonly _http = inject(HttpClient);

  employeeDay(employeeId: string, date: string): Observable<DaySchedule> {
    return this._http.get<DaySchedule>(`${EMPLOYEES_URL}/${encodeURIComponent(employeeId)}/${SCHEDULE_SEGMENT}`, {
      params: new HttpParams().set(DATE_PARAMETER, date),
      context: new HttpContext().set(SKIP_ERROR_TOAST, true),
    });
  }

  companyDay(date: string): Observable<CompanyDaySchedule> {
    return this._http.get<CompanyDaySchedule>(COMPANY_SCHEDULE_URL, {
      params: new HttpParams().set(DATE_PARAMETER, date),
    });
  }
}
