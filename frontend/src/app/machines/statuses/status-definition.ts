import type { DateRangeBody, SetFilterBody, TextFilterBody } from '@shared/grid/requests/grid-filters';
import type { GridPage, GridQueryState, GridRequestBody, GridSort } from '@shared/grid/requests/grid-request';
import type { PlainDateRange } from '@shared/time/plain-date-range';

export enum StatusSeverity {
  Info = 'info',
  Warning = 'warning',
  Error = 'error',
}

export enum StatusDefinitionColumn {
  Code = 'code',
  Severity = 'severity',
  CreatedAt = 'createdAt',
  UpdatedAt = 'updatedAt',
}

/** The columns that the backend sorts by. */
export enum StatusDefinitionSortColumn {
  Code = 'code',
  Severity = 'severity',
  CreatedAt = 'createdAt',
  UpdatedAt = 'updatedAt',
}

export interface StatusDefinition {
  readonly id: string;
  readonly code: string;
  readonly severity: StatusSeverity;
  readonly description: string | null;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface StatusDefinitionBody {
  readonly code: string;
  readonly severity: StatusSeverity;
  readonly description: string | null;
}

/** The batch of status definitions: each item creates the status with its code or changes the one that has it. */
export interface ApplyStatusDefinitionsBody {
  readonly items: readonly StatusDefinitionBatchItem[];
}

/** An item of the batch. A `null` severity or description keeps the stored value of a status that exists. */
export interface StatusDefinitionBatchItem {
  readonly code: string;
  readonly severity: StatusSeverity | null;
  readonly description: string | null;
}

export interface CreateStatusDefinitionResponse {
  readonly id: string;
}

export type StatusDefinitionsPage = GridPage<StatusDefinition>;

export type StatusDefinitionSort = GridSort<StatusDefinitionSortColumn>;

export interface StatusDefinitionFilter {
  readonly code: string;
  readonly severities: readonly StatusSeverity[];
  readonly createdAt: PlainDateRange;
  readonly updatedAt: PlainDateRange;
}

export type StatusDefinitionGridState = GridQueryState<StatusDefinitionFilter, StatusDefinitionSortColumn>;

export interface StatusDefinitionFilterBody {
  readonly code?: TextFilterBody;
  readonly severities?: SetFilterBody<StatusSeverity>;
  readonly createdAt?: DateRangeBody;
  readonly updatedAt?: DateRangeBody;
}

export type SearchStatusDefinitionsBody = GridRequestBody<StatusDefinitionFilterBody, StatusDefinitionSortColumn>;
