export const Permission = {
  MachinesRead: 'machines:read',
  MachinesWrite: 'machines:write',
  MachinesDelete: 'machines:delete',
  MetricDefinitionsRead: 'metric-definitions:read',
  MetricDefinitionsWrite: 'metric-definitions:write',
  StatusDefinitionsRead: 'status-definitions:read',
  StatusDefinitionsWrite: 'status-definitions:write',
  StatusDefinitionsDelete: 'status-definitions:delete',
  FilesRead: 'files:read',
  FilesWrite: 'files:write',
  FilesDelete: 'files:delete',
  CompanyFilesWrite: 'company-files:write',
  CompanyFilesDelete: 'company-files:delete',
  IngestRead: 'ingest:read',
  IngestWrite: 'ingest:write',
  DraftsRead: 'drafts:read',
  DraftsWrite: 'drafts:write',
  EmployeesRead: 'employees:read',
  EmployeesInvite: 'employees:invite',
  EmployeesManage: 'employees:manage',
  WorkOrdersRead: 'work-orders:read',
  WorkOrdersWrite: 'work-orders:write',
  WorkOrdersReadCompany: 'work-orders:read-company',
  WorkOrdersWriteCompany: 'work-orders:write-company',
  ShiftsRead: 'shifts:read',
  ShiftsReadCompany: 'shifts:read-company',
  ShiftsWriteCompany: 'shifts:write-company',
  OperatingPhasesRead: 'operating-phases:read',
  OperatingPhasesWrite: 'operating-phases:write',
  SignalsRead: 'signals:read',
  CasesRead: 'cases:read',
  CasesWrite: 'cases:write',
} as const;

export type Permission = (typeof Permission)[keyof typeof Permission];

export function readPermissions(idTokenParsed: object | undefined): readonly string[] {
  const claim = (idTokenParsed as { permissions?: unknown } | undefined)?.permissions;
  return Array.isArray(claim) ? claim.filter((value): value is string => typeof value === 'string') : [];
}
