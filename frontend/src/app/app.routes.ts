import type { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    title: 'TITLES.HOME',
    loadChildren: () => import('@home/home.routes').then((m) => m.HOME_ROUTES),
  },
  {
    path: '',
    loadChildren: () => import('@fallback/fallback.routes').then((m) => m.FALLBACK_ROUTES),
  },
];
