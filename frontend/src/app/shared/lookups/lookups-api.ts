import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { CASES_SEARCH_URL } from '@maintenance/cases-api';
import { CaseSortColumn, type SearchCasesBody } from '@maintenance/case';
import { SortDirection } from '@shared/grid/requests/grid-request';
import type { LookupOption } from '@shared/lookups/lookup-options';

const EMPLOYEES_LOOKUP_URL = '/api/core/v1/employees/lookup';
const MACHINES_LOOKUP_URL = '/api/core/v1/machines/lookup';
const WORK_ORDERS_LOOKUP_URL = '/api/core/v1/work-orders/lookup';
const FILES_LOOKUP_URL = '/api/document/v1/files/lookup';

/** The most cases that a mention search lists. */
const MENTION_CASES_LIMIT = 50;

/** The part of a case that a mention shows. */
export interface MentionCase {
  readonly id: string;
  readonly machineId: string;
  readonly openedAt: string;
}

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

  /** The names of the machines with the IDs, also deleted ones. Send at most 100 IDs. */
  machinesByIds(ids: readonly string[]): Observable<LookupOption[]> {
    return this._http
      .post<{ items: LookupOption[] }>(MACHINES_LOOKUP_URL, { ids })
      .pipe(map((response) => response.items));
  }

  /** The work orders of the access scope whose title starts with the text. */
  workOrders(search: string): Observable<LookupOption[]> {
    return this._lookup(WORK_ORDERS_LOOKUP_URL, search);
  }

  /** The files whose name starts with the text. */
  files(search: string): Observable<LookupOption[]> {
    return this._http
      .post<{ files: { id: string; fileName: string }[] }>(FILES_LOOKUP_URL, { search })
      .pipe(
        map((response) =>
          response.files.map((file) => ({ id: file.id, displayName: file.fileName, isDeleted: false })),
        ),
      );
  }

  /** The newest cases of the machines: the ID, the machine, and the opening time. */
  casesOfMachines(machineIds: readonly string[]): Observable<MentionCase[]> {
    const body: SearchCasesBody = {
      offset: 0,
      limit: MENTION_CASES_LIMIT,
      sort: { column: CaseSortColumn.OpenedAt, direction: SortDirection.Descending },
      filter: { machineIds: { ids: machineIds } },
    };
    return this._http.post<{ items: MentionCase[] }>(CASES_SEARCH_URL, body).pipe(map((page) => page.items));
  }

  private _lookup(url: string, search: string): Observable<LookupOption[]> {
    return this._http.post<{ items: LookupOption[] }>(url, { search }).pipe(map((response) => response.items));
  }
}
