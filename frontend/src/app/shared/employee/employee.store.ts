import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { signalStore, withComputed, withProps } from '@ngrx/signals';
import { catchError, of, throwError } from 'rxjs';

import { AuthStore } from '@shared/security/auth.store';

import { EmployeeApi } from './employee-api';

/** Holds the employee of the signed-in user. A user without an employee (`404`) gives `null`. */
export const EmployeeStore = signalStore(
  { providedIn: 'root' },
  withProps(() => {
    const auth = inject(AuthStore);
    const api = inject(EmployeeApi);
    return {
      employee: rxResource({
        params: () => (auth.isAuthenticated() ? {} : undefined),
        stream: () =>
          api
            .current()
            .pipe(
              catchError((error: unknown) =>
                error instanceof HttpErrorResponse && error.status === HttpStatusCode.NotFound
                  ? of(null)
                  : throwError(() => error),
              ),
            ),
      }),
    };
  }),
  withComputed((store) => {
    const current = computed(() => (store.employee.hasValue() ? store.employee.value() : null));
    return {
      id: computed(() => current()?.id ?? null),
      name: computed(() => {
        const employee = current();
        return employee === null ? null : `${employee.firstName} ${employee.lastName}`;
      }),
      role: computed(() => current()?.roleCode ?? null),
      hasEmployee: computed(() => current() !== null),
    };
  }),
);
