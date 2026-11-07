import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type { MetricUnitsResponse } from '@machines/metrics/metric-definition';

const METRIC_UNITS_URL = '/api/telemetry/v1/metric-units';

/** Reads the unit catalog. Provide it on the feature route. */
@Injectable()
export class MetricUnitsApi {
  private readonly _http = inject(HttpClient);

  getUnits(): Observable<MetricUnitsResponse> {
    return this._http.get<MetricUnitsResponse>(METRIC_UNITS_URL);
  }
}
