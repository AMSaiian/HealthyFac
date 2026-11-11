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
} as const;

export type Permission = (typeof Permission)[keyof typeof Permission];

export function readPermissions(idTokenParsed: object | undefined): readonly string[] {
  const claim = (idTokenParsed as { permissions?: unknown } | undefined)?.permissions;
  return Array.isArray(claim) ? claim.filter((value): value is string => typeof value === 'string') : [];
}
