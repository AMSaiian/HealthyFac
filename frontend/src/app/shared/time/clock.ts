import { InjectionToken } from '@angular/core';

import { Temporal } from './temporal';

export interface Clock {
  now(): Temporal.Instant;
}

export const CLOCK = new InjectionToken<Clock>('CLOCK', {
  providedIn: 'root',
  factory: () => ({ now: () => Temporal.Now.instant() }),
});
