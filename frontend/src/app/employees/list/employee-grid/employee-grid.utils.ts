import { asList, asText } from '@shared/grid/table/table-filters';
import { EmployeeRole } from '@shared/employee/employee';

import { EmployeeColumn, type EmployeeFilter, EmployeeSortColumn, EmployeeStatus } from '@employees/employee';

export const COLUMN_LABEL_KEYS: Readonly<Record<EmployeeColumn, string>> = {
  [EmployeeColumn.Name]: 'EMPLOYEES.COLUMNS.NAME',
  [EmployeeColumn.Email]: 'EMPLOYEES.COLUMNS.EMAIL',
  [EmployeeColumn.Role]: 'EMPLOYEES.COLUMNS.ROLE',
  [EmployeeColumn.Status]: 'EMPLOYEES.COLUMNS.STATUS',
  [EmployeeColumn.CreatedAt]: 'EMPLOYEES.COLUMNS.CREATED_AT',
};

export const ROLE_LABEL_KEYS: Readonly<Record<EmployeeRole, string>> = {
  [EmployeeRole.Worker]: 'EMPLOYEES.ROLES.WORKER',
  [EmployeeRole.Supervisor]: 'EMPLOYEES.ROLES.SUPERVISOR',
};

export const STATUS_LABEL_KEYS: Readonly<Record<EmployeeStatus, string>> = {
  [EmployeeStatus.Provisioning]: 'EMPLOYEES.STATUSES.PROVISIONING',
  [EmployeeStatus.Active]: 'EMPLOYEES.STATUSES.ACTIVE',
  [EmployeeStatus.ProvisioningFailed]: 'EMPLOYEES.STATUSES.PROVISIONING_FAILED',
  [EmployeeStatus.Deactivated]: 'EMPLOYEES.STATUSES.DEACTIVATED',
};

export const MENU_KEYS = {
  shifts: 'EMPLOYEES.MENU.SHIFTS',
  schedule: 'EMPLOYEES.MENU.SCHEDULE',
  retry: 'EMPLOYEES.MENU.RETRY_PROVISIONING',
} as const;

export const SORTABLE_COLUMNS: readonly string[] = Object.values(EmployeeSortColumn);
export type FilterableColumn = EmployeeColumn.Name | EmployeeColumn.Email | EmployeeColumn.Role | EmployeeColumn.Status;

export const FILTERABLE_COLUMNS: readonly FilterableColumn[] = [
  EmployeeColumn.Name,
  EmployeeColumn.Email,
  EmployeeColumn.Role,
  EmployeeColumn.Status,
];

export const FILTER_READERS: Readonly<Record<FilterableColumn, (value: unknown) => Partial<EmployeeFilter>>> = {
  [EmployeeColumn.Name]: (value) => ({ name: asText(value) }),
  [EmployeeColumn.Email]: (value) => ({ email: asText(value) }),
  [EmployeeColumn.Role]: (value) => ({ roles: asList<EmployeeRole>(value) }),
  [EmployeeColumn.Status]: (value) => ({ statuses: asList<EmployeeStatus>(value) }),
};

export const FILTER_WRITERS: Readonly<Record<FilterableColumn, (filter: EmployeeFilter) => unknown>> = {
  [EmployeeColumn.Name]: (filter) => filter.name || null,
  [EmployeeColumn.Email]: (filter) => filter.email || null,
  [EmployeeColumn.Role]: (filter) => (filter.roles.length > 0 ? [...filter.roles] : null),
  [EmployeeColumn.Status]: (filter) => (filter.statuses.length > 0 ? [...filter.statuses] : null),
};

/** The readers of every column: the creation date has no filter, so its reader ignores the value. */
export const ALL_FILTER_READERS: Readonly<Record<EmployeeColumn, (value: unknown) => Partial<EmployeeFilter>>> = {
  ...FILTER_READERS,
  [EmployeeColumn.CreatedAt]: () => ({}),
};

export const ALL_FILTER_WRITERS: Readonly<Record<EmployeeColumn, (filter: EmployeeFilter) => unknown>> = {
  ...FILTER_WRITERS,
  [EmployeeColumn.CreatedAt]: () => null,
};

export const MENU_TOOLTIP_KEYS = {
  shifts: 'EMPLOYEES.TOOLTIPS.SHIFTS',
  schedule: 'EMPLOYEES.TOOLTIPS.SCHEDULE',
  retry: 'EMPLOYEES.TOOLTIPS.RETRY_PROVISIONING',
} as const;
