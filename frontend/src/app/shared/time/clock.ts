import { inject, InjectionToken } from '@angular/core';

import { CompanyStore } from '@shared/company/company.store';

import { Temporal } from './temporal';

export interface Clock {
  now(): Temporal.Instant;
  timeZoneId(): string;
}

/** The clock of the app. The zone is the company zone, and `companyGuard` loads it before any protected route. */
export const CLOCK = new InjectionToken<Clock>('CLOCK', {
  providedIn: 'root',
  factory: () => {
    const company = inject(CompanyStore);
    return {
      now: () => Temporal.Now.instant(),
      timeZoneId: () => {
        const timeZoneId = company.timeZoneId();
        if (timeZoneId === null) {
          throw new Error('The company time zone is not loaded.');
        }
        return timeZoneId;
      },
    };
  },
});
