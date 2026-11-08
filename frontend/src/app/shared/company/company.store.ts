import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { signalStore, withComputed, withProps } from '@ngrx/signals';

import { AuthStore } from '@shared/security/auth.store';

import { CompanyApi } from './company-api';

export const CompanyStore = signalStore(
  { providedIn: 'root' },
  withProps(() => {
    const auth = inject(AuthStore);
    const api = inject(CompanyApi);
    return {
      company: rxResource({
        params: () => (auth.isAuthenticated() ? {} : undefined),
        stream: () => api.current(),
      }),
    };
  }),
  withComputed((store) => ({
    timeZoneId: computed((): string | null => (store.company.hasValue() ? store.company.value().timeZoneId : null)),
  })),
);
