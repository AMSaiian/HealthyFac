import { HttpClient, type HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type {
  CaseChildrenBody,
  CaseDetail,
  CaseRunsPage,
  CaseSignalsPage,
  CasesPage,
  CaseToolCallsResponse,
  CloseCaseBody,
  SearchCasesBody,
} from '@maintenance/case';

const CASES_URL = '/api/insights/v1/cases';
export const CASES_SEARCH_URL = `${CASES_URL}/search`;
const RUNS_SEGMENT = 'runs';
const CLOSE_SEGMENT = 'close';
const SIGNALS_SEARCH_SEGMENT = 'signals/search';
const RUNS_SEARCH_SEGMENT = 'runs/search';
const TOOL_CALLS_SEGMENT = 'tool-calls';

interface RequestOptions {
  readonly context?: HttpContext;
}

function caseUrl(id: string): string {
  return `${CASES_URL}/${encodeURIComponent(id)}`;
}

/** Owns every HTTP call of the maintenance cases. Provide it on the feature route. */
@Injectable()
export class CasesApi {
  private readonly _http = inject(HttpClient);

  search(body: SearchCasesBody): Observable<CasesPage> {
    return this._http.post<CasesPage>(CASES_SEARCH_URL, body);
  }

  get(id: string, options?: RequestOptions): Observable<CaseDetail> {
    return this._http.get<CaseDetail>(caseUrl(id), options);
  }

  /** One page of the signals of the case: the open ones first, then the newest. */
  searchSignals(id: string, body: CaseChildrenBody): Observable<CaseSignalsPage> {
    return this._http.post<CaseSignalsPage>(`${caseUrl(id)}/${SIGNALS_SEARCH_SEGMENT}`, body);
  }

  /** One page of the runs of the case, the newest first. The rows hold no tool calls. */
  searchRuns(id: string, body: CaseChildrenBody): Observable<CaseRunsPage> {
    return this._http.post<CaseRunsPage>(`${caseUrl(id)}/${RUNS_SEARCH_SEGMENT}`, body);
  }

  getToolCalls(id: string, runId: string): Observable<CaseToolCallsResponse> {
    return this._http.get<CaseToolCallsResponse>(
      `${caseUrl(id)}/${RUNS_SEGMENT}/${encodeURIComponent(runId)}/${TOOL_CALLS_SEGMENT}`,
    );
  }

  /** Queues a new run of the case. Answers `422` when the case is not open. */
  runAgain(id: string, options?: RequestOptions): Observable<void> {
    return this._http.post<void>(`${caseUrl(id)}/${RUNS_SEGMENT}`, null, options);
  }

  /** Closes the case with the reason. Answers `422` when the case is not open. */
  close(id: string, body: CloseCaseBody, options?: RequestOptions): Observable<void> {
    return this._http.post<void>(`${caseUrl(id)}/${CLOSE_SEGMENT}`, body, options);
  }
}
