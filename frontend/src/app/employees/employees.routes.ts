import type { Routes } from '@angular/router';

import { permissionGuard } from '@shared/security/permission.guard';
import { Permission } from '@shared/security/permissions';

import { EmployeesApi } from '@employees/employees-api';
import { InvitesGridStore } from '@employees/invites/invites-grid.store';
import { EmployeesGridStore } from '@employees/list/employees-grid.store';
import { EmployeesPage } from '@employees/list/employees-page/employees-page';
import { ShiftsPage } from '@employees/shifts/shifts-page/shifts-page';
import { ShiftsStore } from '@employees/shifts/shifts-store/shifts.store';

export const EMPLOYEES_ROUTES: Routes = [
  {
    path: '',
    title: 'TITLES.EMPLOYEES',
    component: EmployeesPage,
    canActivate: [permissionGuard(Permission.EmployeesRead)],
    providers: [EmployeesApi, EmployeesGridStore, InvitesGridStore],
  },
  {
    path: ':id/shifts',
    title: 'TITLES.SHIFTS',
    component: ShiftsPage,
    canActivate: [permissionGuard(Permission.ShiftsRead)],
    providers: [EmployeesApi, ShiftsStore],
  },
];
