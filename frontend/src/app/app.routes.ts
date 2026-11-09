import type { Routes } from '@angular/router';

import { companyGuard } from '@shared/company/company.guard';
import { authGuard } from '@shared/security/auth.guard';

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
    path: 'files',
    title: 'TITLES.FILES',
    canActivate: [authGuard, companyGuard],
    loadChildren: () => import('@files/files.routes').then((m) => m.FILES_ROUTES),
  },
  {
    path: '',
    loadChildren: () => import('@fallback/fallback.routes').then((m) => m.FALLBACK_ROUTES),
  },
];
