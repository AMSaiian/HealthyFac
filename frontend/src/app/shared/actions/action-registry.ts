import type { Provider } from '@angular/core';
import { InjectionToken } from '@angular/core';

import type { ActionHostRegistration } from './app-action';

export const ACTION_HOSTS = new InjectionToken<readonly ActionHostRegistration[]>('ACTION_HOSTS', {
  providedIn: 'root',
  factory: () => [],
});

export function provideActionHosts(registrations: readonly ActionHostRegistration[]): Provider {
  return { provide: ACTION_HOSTS, useValue: registrations };
}
