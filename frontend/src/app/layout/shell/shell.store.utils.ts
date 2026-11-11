import type { MenuItem } from 'primeng/api';

import type { Language } from '@shared/i18n/i18n.constants';
import { Permission } from '@shared/security/permissions';

export interface ShellNavItem {
  readonly labelKey: string;
  readonly icon: string;
  readonly path: string;
  readonly permission?: Permission;
  readonly isOwnSchedule?: boolean;
}

const OWN_SCHEDULE_PATH = (employeeId: string): string => `/employees/${encodeURIComponent(employeeId)}/schedule`;

export const SHELL_NAV_ITEMS: readonly ShellNavItem[] = [
  { labelKey: 'TITLES.HOME', icon: 'pi pi-home', path: '/' },
  { labelKey: 'TITLES.MACHINES', icon: 'pi pi-cog', path: '/machines', permission: Permission.MachinesRead },
  {
    labelKey: 'TITLES.WORK_ORDERS',
    icon: 'pi pi-clipboard',
    path: '/work-orders',
    permission: Permission.WorkOrdersRead,
  },
  { labelKey: 'TITLES.EMPLOYEES', icon: 'pi pi-users', path: '/employees', permission: Permission.EmployeesRead },
  {
    labelKey: 'TITLES.COMPANY_SCHEDULE',
    icon: 'pi pi-calendar',
    path: '/company/schedule',
    permission: Permission.ShiftsManage,
  },
  {
    labelKey: 'TITLES.MY_SCHEDULE',
    icon: 'pi pi-calendar-clock',
    path: '',
    permission: Permission.ShiftsRead,
    isOwnSchedule: true,
  },
  { labelKey: 'TITLES.FILES', icon: 'pi pi-folder', path: '/files', permission: Permission.FilesRead },
];

/**
 * Keeps the items that the user may open. The own schedule item needs the employee of the user: it points to the
 * schedule of that employee and stays hidden for a user without one.
 */
export function visibleNavItems(
  items: readonly ShellNavItem[],
  hasPermission: (permission: Permission) => boolean,
  ownEmployeeId: string | null,
): readonly ShellNavItem[] {
  return items
    .filter((item) => item.permission === undefined || hasPermission(item.permission))
    .filter((item) => item.isOwnSchedule !== true || ownEmployeeId !== null)
    .map((item) =>
      item.isOwnSchedule === true && ownEmployeeId !== null
        ? { ...item, path: OWN_SCHEDULE_PATH(ownEmployeeId) }
        : item,
    );
}

/**
 * The items of the language menu: one for each language, with the translation key of its name as the label, its flag
 * in `state`, and a mark on the current one.
 */
export function languageMenuItems(
  languages: readonly Language[],
  current: Language,
  flagUrl: (language: Language) => string,
  choose: (language: Language) => void,
): MenuItem[] {
  return languages.map((language) => ({
    id: language,
    label: `LANGUAGE.${language.toUpperCase()}`,
    state: { flagUrl: flagUrl(language), isCurrent: language === current },
    command: (): void => choose(language),
  }));
}
