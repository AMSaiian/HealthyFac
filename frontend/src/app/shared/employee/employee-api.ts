import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

const DEPARTMENTS_URL = '/api/core/v1/departments';

export interface DepartmentOption {
  readonly id: string;
  readonly name: string;
}

export interface DepartmentsResponse {
  readonly items: readonly DepartmentOption[];
}

/** Owns the HTTP calls about the current employee. */
@Injectable({ providedIn: 'root' })
export class EmployeeApi {
  private readonly _http = inject(HttpClient);

  departments(): Observable<DepartmentsResponse> {
    return this._http.get<DepartmentsResponse>(DEPARTMENTS_URL);
  }
}
