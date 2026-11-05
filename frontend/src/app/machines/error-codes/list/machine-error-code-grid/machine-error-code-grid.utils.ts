import { asRange, asText } from '@shared/grid/table/table-filters';
import { isRangeEmpty } from '@shared/time/plain-date-range';

import {
  MachineErrorCodeColumn,
  type MachineErrorCodeFilter,
  MachineErrorCodeSortColumn,
} from '@machines/error-codes/machine-error-code';

export const COLUMN_LABEL_KEYS: Readonly<Record<MachineErrorCodeColumn, string>> = {
  [MachineErrorCodeColumn.Code]: 'MACHINE_ERROR_CODES.COLUMNS.CODE',
  [MachineErrorCodeColumn.CreatedAt]: 'MACHINE_ERROR_CODES.COLUMNS.CREATED_AT',
  [MachineErrorCodeColumn.UpdatedAt]: 'MACHINE_ERROR_CODES.COLUMNS.UPDATED_AT',
};

export const MENU_UPDATE_KEY = 'MACHINE_ERROR_CODES.UPDATE';
export const MENU_DELETE_KEY = 'MACHINE_ERROR_CODES.DELETE';

export const SORTABLE_COLUMNS: readonly string[] = Object.values(MachineErrorCodeSortColumn);

/** Maps the pending value of one column filter of the table to the store filter. A missing value means "empty". */
export const FILTER_READERS: Readonly<
  Record<MachineErrorCodeColumn, (value: unknown) => Partial<MachineErrorCodeFilter>>
> = {
  [MachineErrorCodeColumn.Code]: (value) => ({ code: asText(value) }),
  [MachineErrorCodeColumn.CreatedAt]: (value) => ({ createdAt: asRange(value) }),
  [MachineErrorCodeColumn.UpdatedAt]: (value) => ({ updatedAt: asRange(value) }),
};

/** Reads the applied store value of one column as a table filter value. `null` means "no filter". */
export const FILTER_WRITERS: Readonly<Record<MachineErrorCodeColumn, (filter: MachineErrorCodeFilter) => unknown>> = {
  [MachineErrorCodeColumn.Code]: (filter) => filter.code || null,
  [MachineErrorCodeColumn.CreatedAt]: (filter) => (isRangeEmpty(filter.createdAt) ? null : filter.createdAt),
  [MachineErrorCodeColumn.UpdatedAt]: (filter) => (isRangeEmpty(filter.updatedAt) ? null : filter.updatedAt),
};
