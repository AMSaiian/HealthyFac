import { asList, asRange, asText } from '@shared/grid/table/table-filters';
import { isRangeEmpty } from '@shared/time/plain-date-range';

import { MachineColumn, type MachineFilter, MachineSortColumn, MachineState } from '@machines/machine';

export const COLUMN_LABEL_KEYS: Readonly<Record<MachineColumn, string>> = {
  [MachineColumn.Name]: 'MACHINES.COLUMNS.NAME',
  [MachineColumn.SerialNumber]: 'MACHINES.COLUMNS.SERIAL_NUMBER',
  [MachineColumn.Department]: 'MACHINES.COLUMNS.DEPARTMENT',
  [MachineColumn.State]: 'MACHINES.COLUMNS.STATE',
  [MachineColumn.CreatedAt]: 'MACHINES.COLUMNS.CREATED_AT',
  [MachineColumn.UpdatedAt]: 'MACHINES.COLUMNS.UPDATED_AT',
};

export const STATE_LABEL_KEYS: Readonly<Record<MachineState, string>> = {
  [MachineState.Operational]: 'MACHINES.STATES.OPERATIONAL',
  [MachineState.InMaintenance]: 'MACHINES.STATES.IN_MAINTENANCE',
  [MachineState.OutOfService]: 'MACHINES.STATES.OUT_OF_SERVICE',
};

export const MENU_ERROR_CODES_KEY = 'MACHINES.ERROR_CODES';
export const MENU_UPDATE_KEY = 'MACHINES.UPDATE';
export const MENU_DELETE_KEY = 'MACHINES.DELETE';

export const SORTABLE_COLUMNS: readonly string[] = Object.values(MachineSortColumn);

/** Maps the pending value of one column filter of the table to the store filter. A missing value means "empty". */
export const FILTER_READERS: Readonly<Record<MachineColumn, (value: unknown) => Partial<MachineFilter>>> = {
  [MachineColumn.Name]: (value) => ({ name: asText(value) }),
  [MachineColumn.SerialNumber]: (value) => ({ serialNumber: asText(value) }),
  [MachineColumn.Department]: (value) => ({ departmentIds: asList<string>(value) }),
  [MachineColumn.State]: (value) => ({ states: asList<MachineState>(value) }),
  [MachineColumn.CreatedAt]: (value) => ({ createdAt: asRange(value) }),
  [MachineColumn.UpdatedAt]: (value) => ({ updatedAt: asRange(value) }),
};

/** Reads the applied store value of one column as a table filter value. `null` means "no filter". */
export const FILTER_WRITERS: Readonly<Record<MachineColumn, (filter: MachineFilter) => unknown>> = {
  [MachineColumn.Name]: (filter) => filter.name || null,
  [MachineColumn.SerialNumber]: (filter) => filter.serialNumber || null,
  [MachineColumn.Department]: (filter) => (filter.departmentIds.length > 0 ? [...filter.departmentIds] : null),
  [MachineColumn.State]: (filter) => (filter.states.length > 0 ? [...filter.states] : null),
  [MachineColumn.CreatedAt]: (filter) => (isRangeEmpty(filter.createdAt) ? null : filter.createdAt),
  [MachineColumn.UpdatedAt]: (filter) => (isRangeEmpty(filter.updatedAt) ? null : filter.updatedAt),
};
