import { HttpClient, type HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type {
  CreateMachineErrorCodeResponse,
  MachineErrorCode,
  MachineErrorCodeBody,
  MachineErrorCodesPage,
  SearchMachineErrorCodesBody,
} from '@machines/error-codes/machine-error-code';

const MACHINES_URL = '/api/core/v1/machines';
const ERROR_CODES_SEGMENT = 'error-codes';
const SEARCH_SEGMENT = 'search';

interface WriteOptions {
  readonly context?: HttpContext;
}

function errorCodesUrl(machineId: string): string {
  return `${MACHINES_URL}/${encodeURIComponent(machineId)}/${ERROR_CODES_SEGMENT}`;
}

function errorCodeUrl(machineId: string, id: string): string {
  return `${errorCodesUrl(machineId)}/${encodeURIComponent(id)}`;
}

/** Owns every HTTP call of the error codes of a machine. Provide it on the feature route and on the dialog host. */
@Injectable()
export class MachineErrorCodesApi {
  private readonly _http = inject(HttpClient);

  search(machineId: string, body: SearchMachineErrorCodesBody): Observable<MachineErrorCodesPage> {
    return this._http.post<MachineErrorCodesPage>(`${errorCodesUrl(machineId)}/${SEARCH_SEGMENT}`, body);
  }

  get(machineId: string, id: string): Observable<MachineErrorCode> {
    return this._http.get<MachineErrorCode>(errorCodeUrl(machineId, id));
  }

  create(
    machineId: string,
    body: MachineErrorCodeBody,
    options?: WriteOptions,
  ): Observable<CreateMachineErrorCodeResponse> {
    return this._http.post<CreateMachineErrorCodeResponse>(errorCodesUrl(machineId), body, options);
  }

  update(machineId: string, id: string, body: MachineErrorCodeBody, options?: WriteOptions): Observable<void> {
    return this._http.put<void>(errorCodeUrl(machineId, id), body, options);
  }

  delete(machineId: string, id: string): Observable<void> {
    return this._http.delete<void>(errorCodeUrl(machineId, id));
  }
}
