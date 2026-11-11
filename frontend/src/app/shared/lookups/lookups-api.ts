import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, type Observable } from 'rxjs';

import type { LookupOption } from '@shared/lookups/lookup-options';

const EMPLOYEES_LOOKUP_URL = '/api/core/v1/employees/lookup';
const MACHINES_LOOKUP_URL = '/api/core/v1/machines/lookup';

/** Reads the ID and the display name of the rows of a picker or a filter. Needs the read permission of the rows. */
@Injectable({ providedIn: 'root' })
export class LookupsApi {
  private readonly _http = inject(HttpClient);

  /** The active employees of the company whose name holds the text. */
  employees(search: string): Observable<LookupOption[]> {
    return this._lookup(EMPLOYEES_LOOKUP_URL, search);
  }

  /** The machines of the company whose name holds the text. */
  machines(search: string): Observable<LookupOption[]> {
    return this._lookup(MACHINES_LOOKUP_URL, search);
  }

  private _lookup(url: string, search: string): Observable<LookupOption[]> {
    return this._http.post<{ items: LookupOption[] }>(url, { search }).pipe(map((response) => response.items));
  }
}
