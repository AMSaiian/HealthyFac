import { HttpClient, type HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type {
  ApplyMetricDefinitionsBody,
  ApplyMetricDefinitionsResponse,
  MetricDefinitionsResponse,
} from '@machines/metrics/metric-definition';

const MACHINES_URL = '/api/telemetry/v1/machines';
const DEFINITIONS_SEGMENT = 'metric-definitions';

interface RequestOptions {
  readonly context?: HttpContext;
}

function machineUrl(machineId: string): string {
  return `${MACHINES_URL}/${encodeURIComponent(machineId)}`;
}

/** Owns every HTTP call of the metric definitions of a machine. Provide it on the feature route. */
@Injectable()
export class MetricDefinitionsApi {
  private readonly _http = inject(HttpClient);

  getDefinitions(machineId: string, options?: RequestOptions): Observable<MetricDefinitionsResponse> {
    return this._http.get<MetricDefinitionsResponse>(`${machineUrl(machineId)}/${DEFINITIONS_SEGMENT}`, options);
  }

  apply(
    machineId: string,
    body: ApplyMetricDefinitionsBody,
    options?: RequestOptions,
  ): Observable<ApplyMetricDefinitionsResponse> {
    return this._http.put<ApplyMetricDefinitionsResponse>(
      `${machineUrl(machineId)}/${DEFINITIONS_SEGMENT}`,
      body,
      options,
    );
  }
}
