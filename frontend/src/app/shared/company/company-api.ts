import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

const COMPANY_URL = '/api/core/v1/company';

export interface CurrentCompany {
  readonly name: string;
  readonly timeZoneId: string;
}

/** Owns the HTTP calls about the company of the signed-in user. */
@Injectable({ providedIn: 'root' })
export class CompanyApi {
  private readonly _http = inject(HttpClient);

  current(): Observable<CurrentCompany> {
    return this._http.get<CurrentCompany>(COMPANY_URL);
  }
}
