import { HttpClient, type HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type { OperatingPhaseBody } from '@machines/phases/operating-phase';
import {
  type ApplyMachineDraftBody,
  type ApplyMachineDraftResponse,
  type ApplyPhaseDraftResponse,
  type AttemptDrafts,
  type DismissMachineDraftsResponse,
  DraftKind,
  type MachineDraftResponse,
  type MachineDraftsPage,
  type SearchMachineDraftsBody,
} from '@machines/drafts/draft';

const CORE_URL = '/api/core/v1';
const TELEMETRY_URL = '/api/telemetry/v1';
const DEFINITION_DRAFTS_URL = `${TELEMETRY_URL}/definition-drafts`;
const MACHINE_DRAFTS_URL = `${CORE_URL}/machine-drafts`;
const DRAFTS_URL = `${CORE_URL}/drafts`;
const MACHINES_URL = `${CORE_URL}/machines`;
const TELEMETRY_MACHINES_URL = `${TELEMETRY_URL}/machines`;
const ATTEMPTS_URL = `${CORE_URL}/ingest-attempts`;
const DRAFTS_SEGMENT = 'drafts';
const SEARCH_SEGMENT = 'search';
const APPLY_SEGMENT = 'apply';
const DISMISS_SEGMENT = 'dismiss';

/** The part of a draft that decides where its dismiss goes. */
export interface DismissTarget {
  readonly id: string;
  readonly kind: DraftKind;
}

interface WriteOptions {
  readonly context?: HttpContext;
}

/** Owns every HTTP call of the machine drafts (Core). Provide it where the feature needs it. */
@Injectable()
export class DraftsApi {
  private readonly _http = inject(HttpClient);

  searchForMachine(
    machineId: string,
    body: SearchMachineDraftsBody,
    options?: WriteOptions,
  ): Observable<MachineDraftsPage> {
    return this._http.post<MachineDraftsPage>(
      `${MACHINES_URL}/${encodeURIComponent(machineId)}/${DRAFTS_SEGMENT}/${SEARCH_SEGMENT}`,
      body,
      options,
    );
  }

  searchForAttempt(
    attemptId: string,
    body: SearchMachineDraftsBody,
    options?: WriteOptions,
  ): Observable<MachineDraftsPage> {
    return this._http.post<MachineDraftsPage>(
      `${ATTEMPTS_URL}/${encodeURIComponent(attemptId)}/${DRAFTS_SEGMENT}/${SEARCH_SEGMENT}`,
      body,
      options,
    );
  }

  /** Dismisses every pending draft of the ingest attempt, on all pages. */
  dismissForAttempt(attemptId: string, options?: WriteOptions): Observable<DismissMachineDraftsResponse> {
    return this._http.post<DismissMachineDraftsResponse>(
      `${ATTEMPTS_URL}/${encodeURIComponent(attemptId)}/${DRAFTS_SEGMENT}/${DISMISS_SEGMENT}`,
      null,
      options,
    );
  }

  listForAttempt(attemptId: string, options?: WriteOptions): Observable<AttemptDrafts> {
    return this._http.get<AttemptDrafts>(`${ATTEMPTS_URL}/${encodeURIComponent(attemptId)}/${DRAFTS_SEGMENT}`, options);
  }

  getMachineDraft(draftId: string, options?: WriteOptions): Observable<MachineDraftResponse> {
    return this._http.get<MachineDraftResponse>(`${MACHINE_DRAFTS_URL}/${encodeURIComponent(draftId)}`, options);
  }

  applyMachineDraft(
    draftId: string,
    body: ApplyMachineDraftBody,
    options?: WriteOptions,
  ): Observable<ApplyMachineDraftResponse> {
    return this._http.post<ApplyMachineDraftResponse>(
      `${MACHINE_DRAFTS_URL}/${encodeURIComponent(draftId)}/${APPLY_SEGMENT}`,
      body,
      options,
    );
  }

  /** Applies a phase draft with the reviewed values. The phase is created in the same save that marks the draft applied. */
  applyPhaseDraft(
    machineId: string,
    draftId: string,
    body: OperatingPhaseBody,
    options?: WriteOptions,
  ): Observable<ApplyPhaseDraftResponse> {
    return this._http.post<ApplyPhaseDraftResponse>(
      `${TELEMETRY_MACHINES_URL}/${encodeURIComponent(machineId)}/definition-drafts/operating-phases/${encodeURIComponent(draftId)}/${APPLY_SEGMENT}`,
      body,
      options,
    );
  }

  /** Dismisses one draft: a machine draft in Core, a metric or status draft in Telemetry. */
  dismiss(draft: DismissTarget, options?: WriteOptions): Observable<void> {
    const baseUrl = draft.kind === DraftKind.Machine ? DRAFTS_URL : DEFINITION_DRAFTS_URL;
    return this._http.post<void>(`${baseUrl}/${encodeURIComponent(draft.id)}/${DISMISS_SEGMENT}`, null, options);
  }

  /** Dismisses every pending draft that targets the machine, on all pages. */
  dismissForMachine(machineId: string, options?: WriteOptions): Observable<DismissMachineDraftsResponse> {
    return this._http.post<DismissMachineDraftsResponse>(
      `${MACHINES_URL}/${encodeURIComponent(machineId)}/${DRAFTS_SEGMENT}/${DISMISS_SEGMENT}`,
      null,
      options,
    );
  }
}
