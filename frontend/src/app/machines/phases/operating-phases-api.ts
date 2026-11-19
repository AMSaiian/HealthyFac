import { HttpClient, type HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type {
  CreateOperatingPhaseResponse,
  OperatingPhaseBody,
  OperatingPhasesResponse,
  ReorderOperatingPhasesBody,
} from '@machines/phases/operating-phase';

const MACHINES_URL = '/api/telemetry/v1/machines';
const PHASES_SEGMENT = 'operating-phases';
const ORDER_SEGMENT = 'order';

interface RequestOptions {
  readonly context?: HttpContext;
}

function phasesUrl(machineId: string): string {
  return `${MACHINES_URL}/${encodeURIComponent(machineId)}/${PHASES_SEGMENT}`;
}

/** Owns every HTTP call of the operating phases of a machine. Provide it on the feature route. */
@Injectable()
export class OperatingPhasesApi {
  private readonly _http = inject(HttpClient);

  list(machineId: string, options?: RequestOptions): Observable<OperatingPhasesResponse> {
    return this._http.get<OperatingPhasesResponse>(phasesUrl(machineId), options);
  }

  create(
    machineId: string,
    body: OperatingPhaseBody,
    options?: RequestOptions,
  ): Observable<CreateOperatingPhaseResponse> {
    return this._http.post<CreateOperatingPhaseResponse>(phasesUrl(machineId), body, options);
  }

  update(machineId: string, id: string, body: OperatingPhaseBody, options?: RequestOptions): Observable<void> {
    return this._http.put<void>(`${phasesUrl(machineId)}/${encodeURIComponent(id)}`, body, options);
  }

  delete(machineId: string, id: string, options?: RequestOptions): Observable<void> {
    return this._http.delete<void>(`${phasesUrl(machineId)}/${encodeURIComponent(id)}`, options);
  }

  /** Sets the order of the phases: the first ID gets the highest priority. */
  reorder(machineId: string, body: ReorderOperatingPhasesBody, options?: RequestOptions): Observable<void> {
    return this._http.put<void>(`${phasesUrl(machineId)}/${ORDER_SEGMENT}`, body, options);
  }
}
