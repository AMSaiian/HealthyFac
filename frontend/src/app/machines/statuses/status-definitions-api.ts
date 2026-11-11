import { HttpClient, type HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type {
  ApplyStatusDefinitionsBody,
  CreateStatusDefinitionResponse,
  SearchStatusDefinitionsBody,
  StatusDefinition,
  StatusDefinitionBody,
  StatusDefinitionsPage,
} from '@machines/statuses/status-definition';

const MACHINES_URL = '/api/telemetry/v1/machines';
const STATUS_DEFINITIONS_SEGMENT = 'status-definitions';
const SEARCH_SEGMENT = 'search';
const APPLY_SEGMENT = 'apply';

interface WriteOptions {
  readonly context?: HttpContext;
}

function statusesUrl(machineId: string): string {
  return `${MACHINES_URL}/${encodeURIComponent(machineId)}/${STATUS_DEFINITIONS_SEGMENT}`;
}

function statusDefinitionUrl(machineId: string, id: string): string {
  return `${statusesUrl(machineId)}/${encodeURIComponent(id)}`;
}

/** Owns every HTTP call of the status definitions of a machine. Provide it on the feature route and on the dialog host. */
@Injectable()
export class StatusDefinitionsApi {
  private readonly _http = inject(HttpClient);

  search(
    machineId: string,
    body: SearchStatusDefinitionsBody,
    options?: WriteOptions,
  ): Observable<StatusDefinitionsPage> {
    return this._http.post<StatusDefinitionsPage>(`${statusesUrl(machineId)}/${SEARCH_SEGMENT}`, body, options);
  }

  /** Creates or changes up to 200 statuses by code in one transaction. */
  apply(machineId: string, body: ApplyStatusDefinitionsBody, options?: WriteOptions): Observable<void> {
    return this._http.post<void>(`${statusesUrl(machineId)}/${APPLY_SEGMENT}`, body, options);
  }

  get(machineId: string, id: string): Observable<StatusDefinition> {
    return this._http.get<StatusDefinition>(statusDefinitionUrl(machineId, id));
  }

  create(
    machineId: string,
    body: StatusDefinitionBody,
    options?: WriteOptions,
  ): Observable<CreateStatusDefinitionResponse> {
    return this._http.post<CreateStatusDefinitionResponse>(statusesUrl(machineId), body, options);
  }

  update(machineId: string, id: string, body: StatusDefinitionBody, options?: WriteOptions): Observable<void> {
    return this._http.put<void>(statusDefinitionUrl(machineId, id), body, options);
  }

  delete(machineId: string, id: string): Observable<void> {
    return this._http.delete<void>(statusDefinitionUrl(machineId, id));
  }
}
