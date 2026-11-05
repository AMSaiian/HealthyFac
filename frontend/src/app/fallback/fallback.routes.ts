import type { Routes } from '@angular/router';

export const FALLBACK_ROUTES: Routes = [
  {
    path: '**',
    title: 'TITLES.NOT_FOUND',
    loadComponent: () => import('@fallback/not-found/not-found-page').then((m) => m.NotFoundPage),
  },
];
