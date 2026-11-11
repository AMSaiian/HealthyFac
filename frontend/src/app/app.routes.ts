import type { Routes } from '@angular/router';

import { companyGuard } from '@shared/company/company.guard';
import { authGuard } from '@shared/security/auth.guard';
import { permissionGuard } from '@shared/security/permission.guard';
import { Permission } from '@shared/security/permissions';

export const routes: Routes = [
  {
    path: '',
    title: 'TITLES.HOME',
    canActivate: [authGuard, companyGuard],
    loadChildren: () => import('@home/home.routes').then((m) => m.HOME_ROUTES),
  },
  {
    path: 'machines',
    title: 'TITLES.MACHINES',
    canActivate: [authGuard, companyGuard],
    loadChildren: () => import('@machines/machines.routes').then((m) => m.MACHINES_ROUTES),
  },
  {
    path: 'work-orders',
    canActivate: [authGuard, companyGuard, permissionGuard(Permission.WorkOrdersRead)],
    loadChildren: () => import('@work-orders/work-orders.routes').then((m) => m.WORK_ORDERS_ROUTES),
  },
  {
    path: 'employees',
    canActivate: [authGuard, companyGuard],
    loadChildren: () => import('@employees/employees.routes').then((m) => m.EMPLOYEES_ROUTES),
  },
  {
    path: 'invite',
    loadChildren: () => import('@invite/invite.routes').then((m) => m.INVITE_ROUTES),
  },
  {
    path: 'files',
    title: 'TITLES.FILES',
    canActivate: [authGuard, companyGuard],
    loadChildren: () => import('@files/files.routes').then((m) => m.FILES_ROUTES),
  },
  {
    path: '',
    canActivate: [authGuard, companyGuard],
    loadChildren: () => import('@schedule/schedule.routes').then((m) => m.SCHEDULE_ROUTES),
  },
  {
    path: '',
    loadChildren: () => import('@fallback/fallback.routes').then((m) => m.FALLBACK_ROUTES),
  },
];
