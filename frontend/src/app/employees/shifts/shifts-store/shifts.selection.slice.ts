import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStoreFeature, withComputed, withMethods, withProps, withState } from '@ngrx/signals';

import { CLOCK } from '@shared/time/clock';
import type { Temporal } from '@shared/time/temporal';

import { EmployeesApi } from '@employees/employees-api';
import { monthRange } from '@employees/shifts/shifts-store/shifts.store.utils';

interface SelectionState {
  employeeId: string | null;
  month: Temporal.PlainDate | null;
}

/** The employee and the month that the shifts page shows, and the versions and overrides of that month. */
export function withShiftsSelection() {
  return signalStoreFeature(
    withState<SelectionState>({ employeeId: null, month: null }),
    withComputed(() => {
      const clock = inject(CLOCK);
      return {
        today: computed(() => clock.now().toZonedDateTimeISO(clock.timeZoneId()).toPlainDate()),
      };
    }),
    withProps((store) => {
      const api = inject(EmployeesApi);
      return {
        versions: rxResource({
          params: () => store.employeeId() ?? undefined,
          stream: ({ params }) => api.shiftVersions(params),
        }),
        overrides: rxResource({
          params: () => {
            const employeeId = store.employeeId();
            const month = store.month();
            return employeeId === null || month === null ? undefined : { employeeId, month };
          },
          stream: ({ params }) => {
            const range = monthRange(params.month);
            return api.shiftOverrides(params.employeeId, range.from, range.to);
          },
        }),
      };
    }),
    withMethods((store) => ({
      setEmployee(employeeId: string): void {
        patchState(store, { employeeId, month: store.month() ?? store.today() });
      },
      previousMonth(): void {
        const month = store.month();
        if (month !== null) {
          patchState(store, { month: month.subtract({ months: 1 }) });
        }
      },
      nextMonth(): void {
        const month = store.month();
        if (month !== null) {
          patchState(store, { month: month.add({ months: 1 }) });
        }
      },
    })),
  );
}
