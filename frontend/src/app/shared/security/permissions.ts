export const Permission = {
  MachinesRead: 'machines:read',
  MachinesWrite: 'machines:write',
  MachinesDelete: 'machines:delete',
} as const;

export type Permission = (typeof Permission)[keyof typeof Permission];

export function readPermissions(idTokenParsed: object | undefined): readonly string[] {
  const claim = (idTokenParsed as { permissions?: unknown } | undefined)?.permissions;
  return Array.isArray(claim) ? claim.filter((value): value is string => typeof value === 'string') : [];
}
