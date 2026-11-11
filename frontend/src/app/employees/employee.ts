import type { SetFilterBody, TextFilterBody } from '@shared/grid/requests/grid-filters';
import type { GridPage, GridQueryState, GridRequestBody, GridSort } from '@shared/grid/requests/grid-request';
import type { EmployeeRole } from '@shared/employee/employee';

export enum EmployeeStatus {
  Provisioning = 'provisioning',
  Active = 'active',
  ProvisioningFailed = 'provisioningFailed',
  Deactivated = 'deactivated',
}

export enum EmployeeColumn {
  Name = 'name',
  Email = 'email',
  Role = 'role',
  Status = 'status',
  CreatedAt = 'createdAt',
}

/** The columns that the backend sorts by. */
export enum EmployeeSortColumn {
  Name = 'name',
  Email = 'email',
  CreatedAt = 'createdAt',
}

export interface EmployeeRow {
  readonly id: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly roleCode: EmployeeRole;
  readonly status: EmployeeStatus;
  readonly createdAt: string;
}

export type EmployeesPage = GridPage<EmployeeRow>;
export type EmployeeSort = GridSort<EmployeeSortColumn>;

export interface EmployeeFilter {
  readonly name: string;
  readonly email: string;
  readonly roles: readonly EmployeeRole[];
  readonly statuses: readonly EmployeeStatus[];
}

export type EmployeeGridState = GridQueryState<EmployeeFilter, EmployeeSortColumn>;

export interface EmployeeFilterBody {
  readonly name?: TextFilterBody;
  readonly email?: TextFilterBody;
  readonly roles?: SetFilterBody<EmployeeRole>;
  readonly statuses?: SetFilterBody<EmployeeStatus>;
}

export type SearchEmployeesBody = GridRequestBody<EmployeeFilterBody, EmployeeSortColumn>;

export enum InviteColumn {
  Email = 'email',
  Role = 'role',
  ExpiresAt = 'expiresAt',
  IsExpired = 'isExpired',
}

/** The columns that the backend sorts by. */
export enum InviteSortColumn {
  ExpiresAt = 'expiresAt',
  Email = 'email',
}

export interface InviteRow {
  readonly id: string;
  readonly email: string;
  readonly roleCode: EmployeeRole;
  readonly expiresAt: string;
  readonly isExpired: boolean;
  readonly rowVersion: number;
}

export type InvitesPage = GridPage<InviteRow>;
export type InviteSort = GridSort<InviteSortColumn>;

export interface InviteFilter {
  readonly email: string;
}

export type InviteGridState = GridQueryState<InviteFilter, InviteSortColumn>;

export interface InviteFilterBody {
  readonly email?: TextFilterBody;
}

export type SearchInvitesBody = GridRequestBody<InviteFilterBody, InviteSortColumn>;

export interface CreateInviteBody {
  readonly email: string;
  readonly roleCode: EmployeeRole;
  readonly firstName?: string;
  readonly lastName?: string;
}

export interface CreateInviteResponse {
  readonly id: string;
}

export interface ResendInviteResponse {
  readonly expiresAt: string;
}
