import { HttpClient, type HttpContext, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, type Observable } from 'rxjs';

import type {
  ApplyScheduleBody,
  ApplyScheduleResponse,
  AssignBody,
  ChangeStatusBody,
  CreateWorkOrderBody,
  CreateWorkOrderResponse,
  DaySchedule,
  FreeSpans,
  LookupFilesResponse,
  MoveBody,
  ScheduleDays,
  SearchWorkOrdersBody,
  UpdateWorkOrderBody,
  WorkOrder,
  WorkOrderHistoryPage,
  WorkOrdersPage,
} from '@work-orders/work-order';

const WORK_ORDERS_URL = '/api/core/v1/work-orders';
const WORK_ORDERS_SEARCH_URL = `${WORK_ORDERS_URL}/search`;
const WORK_ORDERS_SEARCH_BATCH_URL = `${WORK_ORDERS_SEARCH_URL}/batch`;
const FILES_LOOKUP_URL = '/api/document/v1/files/lookup';
const EMPLOYEES_URL = '/api/core/v1/employees';
const SCHEDULE_SEGMENT = 'schedule';
const SCHEDULE_DAYS_SEGMENT = 'days';
const FREE_SPANS_SEGMENT = 'free-spans';
const DATE_PARAMETER = 'date';
const FROM_PARAMETER = 'from';
const TO_PARAMETER = 'to';
const EXCLUDE_WORK_ORDER_PARAMETER = 'excludeWorkOrderId';
const STATUS_SEGMENT = 'status';
const HISTORY_SEGMENT = 'history';
const ASSIGNEE_SEGMENT = 'assignee';
const MOVE_SEGMENT = 'move';
const APPLY_SCHEDULE_URL = '/api/core/v1/work-orders/schedule/apply';
const ROW_VERSION_PARAMETER = 'rowVersion';
const LIMIT_PARAMETER = 'limit';
const AFTER_PARAMETER = 'after';

interface WriteOptions {
  readonly context?: HttpContext;
}

function workOrderUrl(id: string): string {
  return `${WORK_ORDERS_URL}/${encodeURIComponent(id)}`;
}

function rowVersionParams(rowVersion: number): HttpParams {
  return new HttpParams().set(ROW_VERSION_PARAMETER, rowVersion);
}

/**
 * Owns every HTTP call of the work orders feature, including the lookup of attached files at the Document service.
 * Provide it on the feature route and on the action hosts.
 */
@Injectable()
export class WorkOrdersApi {
  private readonly _http = inject(HttpClient);

  search(body: SearchWorkOrdersBody): Observable<WorkOrdersPage> {
    return this._http.post<WorkOrdersPage>(WORK_ORDERS_SEARCH_URL, body);
  }

  /** Reads one page for each search body in one request. The pages come in the order of the bodies. */
  searchBatch(bodies: readonly SearchWorkOrdersBody[]): Observable<readonly WorkOrdersPage[]> {
    return this._http
      .post<{ pages: readonly WorkOrdersPage[] }>(WORK_ORDERS_SEARCH_BATCH_URL, { searches: bodies })
      .pipe(map((response) => response.pages));
  }

  get(id: string, options?: WriteOptions): Observable<WorkOrder> {
    return this._http.get<WorkOrder>(workOrderUrl(id), options);
  }

  /** Reads one page of the history: `limit` rows with a revision below `after`, or the newest rows without it. */
  history(id: string, limit: number, after?: number): Observable<WorkOrderHistoryPage> {
    let params = new HttpParams().set(LIMIT_PARAMETER, limit);
    if (after !== undefined) {
      params = params.set(AFTER_PARAMETER, after);
    }
    return this._http.get<WorkOrderHistoryPage>(`${workOrderUrl(id)}/${HISTORY_SEGMENT}`, { params });
  }

  create(body: CreateWorkOrderBody, options?: WriteOptions): Observable<CreateWorkOrderResponse> {
    return this._http.post<CreateWorkOrderResponse>(WORK_ORDERS_URL, body, options);
  }

  update(id: string, body: UpdateWorkOrderBody, options?: WriteOptions): Observable<void> {
    return this._http.put<void>(workOrderUrl(id), body, options);
  }

  changeStatus(id: string, body: ChangeStatusBody, options?: WriteOptions): Observable<void> {
    return this._http.post<void>(`${workOrderUrl(id)}/${STATUS_SEGMENT}`, body, options);
  }

  assign(id: string, body: AssignBody, options?: WriteOptions): Observable<void> {
    return this._http.put<void>(`${workOrderUrl(id)}/${ASSIGNEE_SEGMENT}`, body, options);
  }

  move(id: string, body: MoveBody, options?: WriteOptions): Observable<void> {
    return this._http.post<void>(`${workOrderUrl(id)}/${MOVE_SEGMENT}`, body, options);
  }

  /** Applies the changes of the schedule tab in one request. All or none. */
  applySchedule(body: ApplyScheduleBody, options?: WriteOptions): Observable<ApplyScheduleResponse> {
    return this._http.post<ApplyScheduleResponse>(APPLY_SCHEDULE_URL, body, options);
  }

  unassign(id: string, rowVersion: number, options?: WriteOptions): Observable<void> {
    return this._http.delete<void>(`${workOrderUrl(id)}/${ASSIGNEE_SEGMENT}`, {
      ...options,
      params: rowVersionParams(rowVersion),
    });
  }

  delete(id: string, rowVersion: number, options?: WriteOptions): Observable<void> {
    return this._http.delete<void>(workOrderUrl(id), { ...options, params: rowVersionParams(rowVersion) });
  }

  /** The day of one employee: the blocks and the scheduled work orders. Needs `shifts:read`. */
  daySchedule(employeeId: string, date: string): Observable<DaySchedule> {
    return this._http.get<DaySchedule>(`${EMPLOYEES_URL}/${encodeURIComponent(employeeId)}/${SCHEDULE_SEGMENT}`, {
      params: new HttpParams().set(DATE_PARAMETER, date),
    });
  }

  /**
   * The summary of each day from `from` to `to` of one employee: the longest free span. `excludeWorkOrderId` leaves
   * the edited work order out of the load. Needs `shifts:read`.
   */
  scheduleDays(employeeId: string, from: string, to: string, excludeWorkOrderId?: string): Observable<ScheduleDays> {
    let params = new HttpParams().set(FROM_PARAMETER, from).set(TO_PARAMETER, to);
    if (excludeWorkOrderId) {
      params = params.set(EXCLUDE_WORK_ORDER_PARAMETER, excludeWorkOrderId);
    }

    return this._http.get<ScheduleDays>(
      `${EMPLOYEES_URL}/${encodeURIComponent(employeeId)}/${SCHEDULE_SEGMENT}/${SCHEDULE_DAYS_SEGMENT}`,
      { params },
    );
  }

  /** The free spans of one day of one employee. `excludeWorkOrderId` leaves the edited work order out. */
  freeSpans(employeeId: string, date: string, excludeWorkOrderId?: string): Observable<FreeSpans> {
    let params = new HttpParams().set(DATE_PARAMETER, date);
    if (excludeWorkOrderId) {
      params = params.set(EXCLUDE_WORK_ORDER_PARAMETER, excludeWorkOrderId);
    }

    return this._http.get<FreeSpans>(
      `${EMPLOYEES_URL}/${encodeURIComponent(employeeId)}/${SCHEDULE_SEGMENT}/${FREE_SPANS_SEGMENT}`,
      { params },
    );
  }

  lookupFiles(ids: readonly string[]): Observable<LookupFilesResponse> {
    return this._http.post<LookupFilesResponse>(FILES_LOOKUP_URL, { ids });
  }
}
