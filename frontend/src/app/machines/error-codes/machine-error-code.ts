import type { DateRangeBody, TextFilterBody } from '@shared/grid/requests/grid-filters';
import type { GridPage, GridQueryState, GridRequestBody, GridSort } from '@shared/grid/requests/grid-request';
import type { PlainDateRange } from '@shared/time/plain-date-range';

export enum MachineErrorCodeColumn {
  Code = 'code',
  CreatedAt = 'createdAt',
  UpdatedAt = 'updatedAt',
}

/** The columns that the backend sorts by. */
export enum MachineErrorCodeSortColumn {
  Code = 'code',
  CreatedAt = 'createdAt',
  UpdatedAt = 'updatedAt',
}

export interface MachineErrorCode {
  readonly id: string;
  readonly code: string;
  readonly description: string | null;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface MachineErrorCodeBody {
  readonly code: string;
  readonly description: string | null;
}

export interface CreateMachineErrorCodeResponse {
  readonly id: string;
}

export type MachineErrorCodesPage = GridPage<MachineErrorCode>;

export type MachineErrorCodeSort = GridSort<MachineErrorCodeSortColumn>;

export interface MachineErrorCodeFilter {
  readonly code: string;
  readonly createdAt: PlainDateRange;
  readonly updatedAt: PlainDateRange;
}

export type MachineErrorCodeGridState = GridQueryState<MachineErrorCodeFilter, MachineErrorCodeSortColumn>;

export interface MachineErrorCodeFilterBody {
  readonly code?: TextFilterBody;
  readonly createdAt?: DateRangeBody;
  readonly updatedAt?: DateRangeBody;
}

export type SearchMachineErrorCodesBody = GridRequestBody<MachineErrorCodeFilterBody, MachineErrorCodeSortColumn>;
