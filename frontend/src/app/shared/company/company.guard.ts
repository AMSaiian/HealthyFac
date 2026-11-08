import { inject } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import type { CanActivateFn } from '@angular/router';
import { filter, map, take } from 'rxjs';

import { CompanyStore } from './company.store';

/**
 * Holds the navigation until the company of the user is loaded. It lets the route activate only when the load
 * succeeded, so no protected view renders before the company time zone is known. A navigation after a failed load
 * retries the load once, so the app recovers without a full page reload.
 */
export const companyGuard: CanActivateFn = () => {
  const { company } = inject(CompanyStore);
  if (company.status() === 'error') {
    company.reload();
  }
  return toObservable(company.status).pipe(
    filter((status) => status === 'resolved' || status === 'error'),
    map(() => company.hasValue()),
    take(1),
  );
};
