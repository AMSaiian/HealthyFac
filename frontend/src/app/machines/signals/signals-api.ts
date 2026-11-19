import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type {
  MetricHistoryResponse,
  PhaseTimesResponse,
  SignalEpisodesResponse,
} from '@machines/signals/signal-episode';

const MACHINES_URL = '/api/telemetry/v1/machines';
const HISTORY_SEGMENT = 'metric-history';
const EPISODES_SEGMENT = 'signal-episodes';
const PHASE_TIMES_SEGMENT = 'phase-times';

function machineUrl(machineId: string): string {
  return `${MACHINES_URL}/${encodeURIComponent(machineId)}`;
}

/** Owns every HTTP call of the signal reads of a machine. Provide it on the feature route. */
@Injectable()
export class SignalsApi {
  private readonly _http = inject(HttpClient);

  /** The history of one metric in `bucketCount` buckets over `[from, to)`. The times are ISO instants. */
  getHistory(
    machineId: string,
    query: { metricDefinitionId: string; from: string; to: string; bucketCount: number },
  ): Observable<MetricHistoryResponse> {
    const params = new HttpParams()
      .set('metricDefinitionId', query.metricDefinitionId)
      .set('from', query.from)
      .set('to', query.to)
      .set('bucketCount', query.bucketCount);
    return this._http.get<MetricHistoryResponse>(`${machineUrl(machineId)}/${HISTORY_SEGMENT}`, { params });
  }

  /** The seconds that the machine spent in each phase over `[from, to)`. The times are ISO instants. */
  getPhaseTimes(machineId: string, range: { from: string; to: string }): Observable<PhaseTimesResponse> {
    const params = new HttpParams().set('from', range.from).set('to', range.to);
    return this._http.get<PhaseTimesResponse>(`${machineUrl(machineId)}/${PHASE_TIMES_SEGMENT}`, { params });
  }

  getEpisodes(machineId: string, range: { from: string; to: string }): Observable<SignalEpisodesResponse> {
    const params = new HttpParams().set('from', range.from).set('to', range.to);
    return this._http.get<SignalEpisodesResponse>(`${machineUrl(machineId)}/${EPISODES_SEGMENT}`, { params });
  }
}
