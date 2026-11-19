import { HttpClient, type HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type {
  ApplyWorkOrderDraftBody,
  DismissWorkOrderDraftBody,
  SearchWorkOrderDraftsBody,
  WorkOrderDraftsPage,
} from '@work-orders/drafts/work-order-draft';

const DRAFTS_URL = '/api/core/v1/work-order-drafts';
const SEARCH_URL = `${DRAFTS_URL}/search`;
const APPLY_SEGMENT = 'apply';
const DISMISS_SEGMENT = 'dismiss';

interface RequestOptions {
  readonly context?: HttpContext;
}

function draftUrl(id: string): string {
  return `${DRAFTS_URL}/${encodeURIComponent(id)}`;
}

/** Owns every HTTP call of the work order drafts. Provide it on the feature route. */
@Injectable()
export class WorkOrderDraftsApi {
  private readonly _http = inject(HttpClient);

  search(body: SearchWorkOrderDraftsBody): Observable<WorkOrderDraftsPage> {
    return this._http.post<WorkOrderDraftsPage>(SEARCH_URL, body);
  }

  /** Creates the work order from the draft. Answers `422` when the draft is not pending, `409` when it changed. */
  apply(id: string, body: ApplyWorkOrderDraftBody, options?: RequestOptions): Observable<unknown> {
    return this._http.post(`${draftUrl(id)}/${APPLY_SEGMENT}`, body, options);
  }

  dismiss(id: string, body: DismissWorkOrderDraftBody, options?: RequestOptions): Observable<void> {
    return this._http.post<void>(`${draftUrl(id)}/${DISMISS_SEGMENT}`, body, options);
  }
}
