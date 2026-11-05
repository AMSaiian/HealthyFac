import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { signalStore, withComputed, withMethods, withProps } from '@ngrx/signals';

import { AuthStore } from '@shared/security/auth.store';

import { type DepartmentOption, EmployeeApi } from './employee-api';

export const EmployeeStore = signalStore(
  { providedIn: 'root' },
  withProps(() => {
    const auth = inject(AuthStore);
    const api = inject(EmployeeApi);
    return {
      departments: rxResource({
        params: () => (auth.isAuthenticated() ? {} : undefined),
        stream: () => api.departments(),
      }),
    };
  }),
  withComputed((store) => ({
    departmentOptions: computed((): readonly DepartmentOption[] =>
      store.departments.hasValue() ? store.departments.value().items : [],
    ),
  })),
  withMethods((store) => ({
    departmentName(id: string): string | undefined {
      return store.departmentOptions().find((department) => department.id === id)?.name;
    },
  })),
);
