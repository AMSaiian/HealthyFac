import type { Routes } from '@angular/router';

import { authGuard } from '@shared/security/auth.guard';

export const routes: Routes = [
  {
    path: '',
    title: 'TITLES.HOME',
    canActivate: [authGuard],
    loadChildren: () => import('@home/home.routes').then((m) => m.HOME_ROUTES),
  },
  {
    path: 'machines',
    title: 'TITLES.MACHINES',
    canActivate: [authGuard],
    loadChildren: () => import('@machines/machines.routes').then((m) => m.MACHINES_ROUTES),
  },
  {
    path: '',
    loadChildren: () => import('@fallback/fallback.routes').then((m) => m.FALLBACK_ROUTES),
  },
];
