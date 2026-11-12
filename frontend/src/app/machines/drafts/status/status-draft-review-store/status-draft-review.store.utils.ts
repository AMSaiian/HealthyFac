import { textFilter } from '@shared/grid/requests/grid-filters';
import { SortDirection } from '@shared/grid/requests/grid-request';

import type { AttemptDrafts, StatusDraftDetail } from '@machines/drafts/draft';
import {
  type SearchStatusDefinitionsBody,
  type StatusDefinition,
  type StatusDefinitionBatchItem,
  StatusDefinitionSortColumn,
  StatusSeverity,
} from '@machines/statuses/status-definition';
import type { StatusDefinitionFormValue } from '@machines/statuses/drawer/status-definition-form/status-definition-form-schema';

export enum StatusCompareField {
  Severity = 'severity',
  Description = 'description',
}

/** Which fields of the proposal the user takes. */
export type StatusTaken = Readonly<Partial<Record<StatusCompareField, boolean>>>;

export interface StatusCompareRow {
  readonly field: StatusCompareField;
  readonly current: string | null;
  /** `null` means "no change": the model proposed nothing for this field. */
  readonly proposed: string | null;
  readonly canTake: boolean;
  readonly isTaken: boolean;
}

/** Finds the status draft with the given ID in the drafts of an attempt. */
export function findStatusDraft(drafts: AttemptDrafts, draftId: string): StatusDraftDetail | null {
  return drafts.groups.flatMap((group) => group.statuses).find((status) => status.id === draftId) ?? null;
}

/**
 * The most statuses that one code search reads. The search matches the start of the code and sorts by code, so the
 * exact code comes before every longer code with the same start: it is in the first rows.
 */
const CODE_SEARCH_LIMIT = 25;

/** The search that finds the status with the code of a draft. Both the review and the quick apply send it. */
export function codeSearchBody(code: string): SearchStatusDefinitionsBody {
  return {
    offset: 0,
    limit: CODE_SEARCH_LIMIT,
    sort: { column: StatusDefinitionSortColumn.Code, direction: SortDirection.Ascending },
    filter: { code: textFilter(code) },
  };
}

/** Finds the active status of the machine with the same code as the draft, ignoring the case. */
export function findStatusByCode(statuses: readonly StatusDefinition[], code: string): StatusDefinition | null {
  const wanted = code.trim().toLowerCase();
  return statuses.find((status) => status.code.trim().toLowerCase() === wanted) ?? null;
}

function toSeverity(value: string | null): StatusSeverity | null {
  return (Object.values(StatusSeverity) as string[]).includes(value ?? '') ? (value as StatusSeverity) : null;
}

/** The form value of a status that does not exist yet: a missing (`null`) field starts empty. */
export function toCreateValue(draft: StatusDraftDetail): StatusDefinitionFormValue {
  return {
    code: draft.code ?? '',
    severity: toSeverity(draft.severity) ?? StatusSeverity.Info,
    description: draft.description,
  };
}

/**
 * One item of the status batch: the values of the draft, as they are. A `null` severity or description means "no
 * proposal": Telemetry keeps the stored value of a status that exists, as the review save does.
 */
export function toBatchChange(draft: StatusDraftDetail): StatusDefinitionBatchItem {
  return { code: (draft.code ?? '').trim(), severity: toSeverity(draft.severity), description: draft.description };
}

/** Builds the compare rows of an existing status. The code is read-only and is not a row. */
export function toStatusCompareRows(
  draft: StatusDraftDetail,
  current: StatusDefinition,
  taken: StatusTaken,
): StatusCompareRow[] {
  const severity = toSeverity(draft.severity);
  return [
    {
      field: StatusCompareField.Severity,
      current: current.severity,
      proposed: severity,
      canTake: severity !== null,
      isTaken: severity !== null && taken.severity === true,
    },
    {
      field: StatusCompareField.Description,
      current: current.description,
      proposed: draft.description,
      canTake: draft.description !== null,
      isTaken: draft.description !== null && taken.description === true,
    },
  ];
}

/** Merges the current status with the taken fields. The code never changes. */
export function mergeStatus(
  current: StatusDefinition,
  draft: StatusDraftDetail,
  rows: readonly StatusCompareRow[],
): StatusDefinitionFormValue {
  const isTaken = (field: StatusCompareField): boolean => rows.some((row) => row.field === field && row.isTaken);
  return {
    code: current.code,
    severity: isTaken(StatusCompareField.Severity)
      ? (toSeverity(draft.severity) ?? current.severity)
      : current.severity,
    description: isTaken(StatusCompareField.Description) ? draft.description : current.description,
  };
}
