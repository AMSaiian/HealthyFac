import { InjectionToken } from '@angular/core';

const PROBE_KEY = 'healthyfac.storage-probe';

export const BROWSER_STORAGE = new InjectionToken<Storage | null>('BROWSER_STORAGE', {
  providedIn: 'root',
  factory: () => {
    try {
      localStorage.setItem(PROBE_KEY, PROBE_KEY);
      localStorage.removeItem(PROBE_KEY);
      return localStorage;
    } catch {
      return null;
    }
  },
});
