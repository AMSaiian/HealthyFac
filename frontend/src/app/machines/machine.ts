import type { DateRangeBody, SetFilterBody, TextFilterBody } from '@shared/grid/requests/grid-filters';
import type { GridPage, GridQueryState, GridRequestBody, GridSort } from '@shared/grid/requests/grid-request';
import type { PlainDateRange } from '@shared/time/plain-date-range';

export enum MachineState {
  Operational = 'operational',
  InMaintenance = 'inMaintenance',
  OutOfService = 'outOfService',
}

export enum MachineColumn {
  Name = 'name',
  SerialNumber = 'serialNumber',
  State = 'state',
  CreatedAt = 'createdAt',
  UpdatedAt = 'updatedAt',
}

/** The columns that the backend sorts by. The state column never sorts. */
export enum MachineSortColumn {
  Name = 'name',
  SerialNumber = 'serialNumber',
  CreatedAt = 'createdAt',
  UpdatedAt = 'updatedAt',
}

export interface Machine {
  readonly id: string;
  readonly name: string;
  readonly serialNumber: string;
  readonly gatewayId: string;
  readonly state: MachineState;
  readonly description: string | null;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface MachineBody {
  readonly name: string;
  readonly serialNumber: string;
  readonly description: string | null;
}

export interface CreateMachineResponse {
  readonly id: string;
}

export type MachinesPage = GridPage<Machine>;

export type MachineSort = GridSort<MachineSortColumn>;

export interface MachineFilter {
  readonly name: string;
  readonly serialNumber: string;
  readonly states: readonly MachineState[];
  readonly createdAt: PlainDateRange;
  readonly updatedAt: PlainDateRange;
}

export type MachineGridState = GridQueryState<MachineFilter, MachineSortColumn>;

export interface MachineFilterBody {
  readonly name?: TextFilterBody;
  readonly serialNumber?: TextFilterBody;
  readonly states?: SetFilterBody<MachineState>;
  readonly createdAt?: DateRangeBody;
  readonly updatedAt?: DateRangeBody;
}

export type SearchMachinesBody = GridRequestBody<MachineFilterBody, MachineSortColumn>;
