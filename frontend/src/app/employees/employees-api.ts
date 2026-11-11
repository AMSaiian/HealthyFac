import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';

import type {
  CreateInviteBody,
  CreateInviteResponse,
  EmployeesPage,
  InvitesPage,
  ResendInviteResponse,
  SearchEmployeesBody,
  SearchInvitesBody,
} from '@employees/employee';
import type {
  CreateVersionBody,
  CreateVersionResponse,
  SetOverrideBody,
  ShiftOverride,
  ShiftVersion,
  UpdateVersionBody,
} from '@employees/shifts/shift';
import { normalizeBlocks, normalizeVersion } from '@employees/shifts/shifts.store.utils';

const EMPLOYEES_URL = '/api/core/v1/employees';
const EMPLOYEES_SEARCH_URL = `${EMPLOYEES_URL}/search`;
const RETRY_PROVISIONING_URL = `${EMPLOYEES_URL}/retry-provisioning`;
const INVITES_URL = '/api/core/v1/employee-invites';
const INVITES_SEARCH_URL = `${INVITES_URL}/search`;
const INVITES_RESEND_URL = `${INVITES_URL}/resend`;
const TEMPLATES_SEGMENT = 'shift-templates';
const OVERRIDES_SEGMENT = 'shift-overrides';
const ROW_VERSION_PARAMETER = 'rowVersion';
const FROM_PARAMETER = 'from';
const TO_PARAMETER = 'to';

const noToast = (): { context: HttpContext } => ({ context: new HttpContext().set(SKIP_ERROR_TOAST, true) });

function employeeUrl(employeeId: string): string {
  return `${EMPLOYEES_URL}/${encodeURIComponent(employeeId)}`;
}

/** Owns the HTTP calls of the employees feature. Provide it on the feature routes. */
@Injectable()
export class EmployeesApi {
  private readonly _http = inject(HttpClient);

  searchEmployees(body: SearchEmployeesBody): Observable<EmployeesPage> {
    return this._http.post<EmployeesPage>(EMPLOYEES_SEARCH_URL, body);
  }

  searchInvites(body: SearchInvitesBody): Observable<InvitesPage> {
    return this._http.post<InvitesPage>(INVITES_SEARCH_URL, body);
  }

  createInvite(body: CreateInviteBody): Observable<CreateInviteResponse> {
    return this._http.post<CreateInviteResponse>(INVITES_URL, body, noToast());
  }

  resendInvite(inviteId: string): Observable<ResendInviteResponse> {
    return this._http.post<ResendInviteResponse>(INVITES_RESEND_URL, { inviteId }, noToast());
  }

  revokeInvite(inviteId: string, rowVersion: number): Observable<void> {
    return this._http.delete<void>(`${INVITES_URL}/${encodeURIComponent(inviteId)}`, {
      ...noToast(),
      params: new HttpParams().set(ROW_VERSION_PARAMETER, rowVersion),
    });
  }

  retryProvisioning(employeeId: string): Observable<void> {
    return this._http.post<void>(RETRY_PROVISIONING_URL, { employeeId }, noToast());
  }

  shiftVersions(employeeId: string): Observable<readonly ShiftVersion[]> {
    return this._http
      .get<readonly ShiftVersion[]>(`${employeeUrl(employeeId)}/${TEMPLATES_SEGMENT}`)
      .pipe(map((versions) => versions.map(normalizeVersion)));
  }

  createVersion(employeeId: string, body: CreateVersionBody): Observable<CreateVersionResponse> {
    return this._http.post<CreateVersionResponse>(`${employeeUrl(employeeId)}/${TEMPLATES_SEGMENT}`, body, noToast());
  }

  updateVersion(employeeId: string, versionId: string, body: UpdateVersionBody): Observable<void> {
    return this._http.put<void>(
      `${employeeUrl(employeeId)}/${TEMPLATES_SEGMENT}/${encodeURIComponent(versionId)}`,
      body,
      noToast(),
    );
  }

  deleteVersion(employeeId: string, versionId: string, rowVersion: number): Observable<void> {
    return this._http.delete<void>(`${employeeUrl(employeeId)}/${TEMPLATES_SEGMENT}/${encodeURIComponent(versionId)}`, {
      ...noToast(),
      params: new HttpParams().set(ROW_VERSION_PARAMETER, rowVersion),
    });
  }

  shiftOverrides(employeeId: string, from: string, to: string): Observable<readonly ShiftOverride[]> {
    return this._http
      .get<readonly ShiftOverride[]>(`${employeeUrl(employeeId)}/${OVERRIDES_SEGMENT}`, {
        params: new HttpParams().set(FROM_PARAMETER, from).set(TO_PARAMETER, to),
      })
      .pipe(map((overrides) => overrides.map((entry) => ({ ...entry, blocks: normalizeBlocks(entry.blocks) }))));
  }

  setOverride(employeeId: string, date: string, body: SetOverrideBody): Observable<void> {
    return this._http.put<void>(`${employeeUrl(employeeId)}/${OVERRIDES_SEGMENT}/${date}`, body, noToast());
  }

  deleteOverride(employeeId: string, date: string, rowVersion: number): Observable<void> {
    return this._http.delete<void>(`${employeeUrl(employeeId)}/${OVERRIDES_SEGMENT}/${date}`, {
      ...noToast(),
      params: new HttpParams().set(ROW_VERSION_PARAMETER, rowVersion),
    });
  }
}
