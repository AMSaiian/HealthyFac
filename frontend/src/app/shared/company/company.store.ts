import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { signalStore, withComputed, withProps } from '@ngrx/signals';

import { AuthStore } from '@shared/security/auth-store/auth.store';

import { CompanyApi } from './company-api';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

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
  withStoreDevtools('company'),
);
