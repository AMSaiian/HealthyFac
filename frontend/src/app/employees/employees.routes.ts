import type { Routes } from '@angular/router';

import { permissionGuard } from '@shared/security/permission.guard';
import { Permission } from '@shared/security/permissions';
import { isOtherParamChanged, tabMatcher } from '@shared/tabs/tab-route';

import { EMPLOYEES_RETURN_TARGET } from '@employees/employees-return-target';
import { EmployeesApi } from '@employees/employees-api';
import { InvitesGridStore } from '@employees/invites/invites-grid.store';
import { EmployeesGridStore } from '@employees/list/employees-grid.store';
import { EmployeesPage } from '@employees/list/employees-page/employees-page';
import { EMPLOYEES_TABS } from '@employees/list/employees-page/employees-page.utils';
import { ShiftsPage } from '@employees/shifts/shifts-page/shifts-page';
import { ShiftsStore } from '@employees/shifts/shifts-store/shifts.store';

import { WorkOrdersApi } from '@work-orders/work-orders-api';

export const EMPLOYEES_ROUTES: Routes = [
  {
    matcher: tabMatcher('', EMPLOYEES_TABS),
    runGuardsAndResolvers: isOtherParamChanged,
    title: 'TITLES.EMPLOYEES',
    component: EmployeesPage,
    canActivate: [permissionGuard(EMPLOYEES_RETURN_TARGET.permission)],
    providers: [EmployeesApi, EmployeesGridStore, InvitesGridStore],
  },
  {
    path: ':id/shifts',
    title: 'TITLES.SHIFTS',
    component: ShiftsPage,
    canActivate: [permissionGuard(Permission.ShiftsRead)],
    providers: [EmployeesApi, WorkOrdersApi, ShiftsStore],
  },
];
