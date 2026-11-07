export const Permission = {
  MachinesRead: 'machines:read',
  MachinesWrite: 'machines:write',
  MachinesDelete: 'machines:delete',
  MetricDefinitionsRead: 'metric-definitions:read',
  MetricDefinitionsWrite: 'metric-definitions:write',
  StatusDefinitionsRead: 'status-definitions:read',
  StatusDefinitionsWrite: 'status-definitions:write',
  StatusDefinitionsDelete: 'status-definitions:delete',
} as const;

export type Permission = (typeof Permission)[keyof typeof Permission];

export function readPermissions(idTokenParsed: object | undefined): readonly string[] {
  const claim = (idTokenParsed as { permissions?: unknown } | undefined)?.permissions;
  return Array.isArray(claim) ? claim.filter((value): value is string => typeof value === 'string') : [];
}
