import { Permission } from '@shared/security/permissions';

export interface ShellNavItem {
  readonly labelKey: string;
  readonly icon: string;
  readonly path: string;
  readonly permission?: Permission;
}

export const SHELL_NAV_ITEMS: readonly ShellNavItem[] = [
  { labelKey: 'TITLES.HOME', icon: 'pi pi-home', path: '/' },
  { labelKey: 'TITLES.MACHINES', icon: 'pi pi-cog', path: '/machines', permission: Permission.MachinesRead },
];
