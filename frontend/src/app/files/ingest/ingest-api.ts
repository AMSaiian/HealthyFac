import { HttpClient, type HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type { IngestAttempt, IngestAttemptsPage, StartIngestResponse } from '@files/ingest/ingest-attempt';

const INSIGHTS_URL = '/api/insights/v1';
const FILES_SEGMENT = 'files';
const FILE_ATTEMPTS_SEGMENT = 'ingest-attempts';
const ATTEMPTS_URL = `${INSIGHTS_URL}/ingest-attempts`;
const CANCEL_SEGMENT = 'cancel';

interface WriteOptions {
  readonly context?: HttpContext;
}

function fileAttemptsUrl(fileId: string): string {
  return `${INSIGHTS_URL}/${FILES_SEGMENT}/${encodeURIComponent(fileId)}/${FILE_ATTEMPTS_SEGMENT}`;
}

function attemptUrl(attemptId: string): string {
  return `${ATTEMPTS_URL}/${encodeURIComponent(attemptId)}`;
}

/** Owns every HTTP call of the ingest attempts (Insights). Provide it where the feature needs it. */
@Injectable()
export class IngestApi {
  private readonly _http = inject(HttpClient);

  start(fileId: string, options?: WriteOptions): Observable<StartIngestResponse> {
    return this._http.post<StartIngestResponse>(fileAttemptsUrl(fileId), null, options);
  }

  cancel(attemptId: string, options?: WriteOptions): Observable<void> {
    return this._http.post<void>(`${attemptUrl(attemptId)}/${CANCEL_SEGMENT}`, null, options);
  }

  listForFile(fileId: string, options?: WriteOptions): Observable<IngestAttemptsPage> {
    return this._http.get<IngestAttemptsPage>(fileAttemptsUrl(fileId), options);
  }

  get(attemptId: string, options?: WriteOptions): Observable<IngestAttempt> {
    return this._http.get<IngestAttempt>(attemptUrl(attemptId), options);
  }
}
