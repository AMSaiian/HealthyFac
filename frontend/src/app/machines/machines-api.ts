import { HttpClient, type HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type { CreateMachineResponse, Machine, MachineBody, MachinesPage, SearchMachinesBody } from '@machines/machine';

const MACHINES_URL = '/api/core/v1/machines';
const MACHINES_SEARCH_URL = `${MACHINES_URL}/search`;

interface WriteOptions {
  readonly context?: HttpContext;
}

function machineUrl(id: string): string {
  return `${MACHINES_URL}/${encodeURIComponent(id)}`;
}

/** Owns every HTTP call of the machines feature. Provide it on the feature route and on the action host. */
@Injectable()
export class MachinesApi {
  private readonly _http = inject(HttpClient);

  search(body: SearchMachinesBody): Observable<MachinesPage> {
    return this._http.post<MachinesPage>(MACHINES_SEARCH_URL, body);
  }

  get(id: string, options?: WriteOptions): Observable<Machine> {
    return this._http.get<Machine>(machineUrl(id), options);
  }

  create(body: MachineBody, options?: WriteOptions): Observable<CreateMachineResponse> {
    return this._http.post<CreateMachineResponse>(MACHINES_URL, body, options);
  }

  update(id: string, body: MachineBody, options?: WriteOptions): Observable<void> {
    return this._http.put<void>(machineUrl(id), body, options);
  }

  delete(id: string): Observable<void> {
    return this._http.delete<void>(machineUrl(id));
  }
}
