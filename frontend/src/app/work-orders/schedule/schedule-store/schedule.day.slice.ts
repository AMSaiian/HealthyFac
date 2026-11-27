import { computed, effect, inject, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import {
  patchState,
  signalStoreFeature,
  withComputed,
  withHooks,
  withMethods,
  withProps,
  withState,
} from '@ngrx/signals';
import { map, type Observable } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import { EmployeeStore } from '@shared/employee/employee.store';
import type { ScheduleRow } from '@shared/schedule/schedule-row';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';
import { CLOCK } from '@shared/time/clock';
import { minuteTicker, todayOf } from '@shared/time/clock-ticks';
import type { Temporal } from '@shared/time/temporal';

import { WorkOrderActionType } from '@work-orders/work-order-actions';
import { type CompanyDaySchedule, ScheduleApi } from '@schedule/schedule-api';
import { followToday, toRow } from '@schedule/schedule-time';

interface ScheduleState {
  date: Temporal.PlainDate | null;
}

interface DayParams {
  readonly date: string;
  readonly employeeId: string | null;
  readonly firstName: string;
  readonly lastName: string;
}

/**
 * One day of the schedule tab. A token with `work-orders:read-company` sees every active employee, and
 * `work-orders:write-company` enables the edits through pending changes; a worker sees the own day as one row. The
 * route provides it, so it resets per visit.
 */
export function withScheduleDay() {
  return signalStoreFeature(
    withState<ScheduleState>({ date: null }),
    withProps(() => ({
      _api: inject(ScheduleApi),
      _clock: inject(CLOCK),
      _actions: inject(ActionStore),
      _auth: inject(AuthStore),
      _employee: inject(EmployeeStore),
    })),
    withComputed((store) => ({
      canManage: computed(() => store._auth.hasPermission(Permission.WorkOrdersWriteCompany)),
      canReadCompany: computed(() => store._auth.hasPermission(Permission.WorkOrdersReadCompany)),
    })),
    withProps((store) => ({
      now: minuteTicker(store._clock),
      day: rxResource({
        params: (): DayParams | undefined => {
          const date = store.date();
          if (date === null) {
            return undefined;
          }
          if (store.canReadCompany()) {
            return { date: date.toString(), employeeId: null, firstName: '', lastName: '' };
          }
          const own = store._employee.employee.hasValue() ? store._employee.employee.value() : null;
          return own === null
            ? undefined
            : { date: date.toString(), employeeId: own.id, firstName: own.firstName, lastName: own.lastName };
        },
        stream: ({ params }): Observable<CompanyDaySchedule> =>
          params.employeeId === null
            ? store._api.companyDay(params.date)
            : store._api.employeeDay(params.employeeId, params.date).pipe(
                map((day) => ({
                  date: params.date,
                  timeZoneId: day.timeZoneId,
                  employees: [
                    {
                      employeeId: params.employeeId ?? '',
                      firstName: params.firstName,
                      lastName: params.lastName,
                      day,
                    },
                  ],
                })),
              ),
      }),
    })),
    withComputed((store) => ({
      hasFailed: computed(() => store.day.status() === 'error'),
      rows: computed((): readonly ScheduleRow[] =>
        store.day.hasValue()
          ? store.day
              .value()
              .employees.map((employee) =>
                toRow(employee.employeeId, `${employee.firstName} ${employee.lastName}`, employee.day),
              )
          : [],
      ),
    })),
    withMethods((store) => ({
      setDate(date: Temporal.PlainDate): void {
        patchState(store, { date });
      },
      previousDay(): void {
        const date = store.date();
        if (date !== null) {
          patchState(store, { date: date.subtract({ days: 1 }) });
        }
      },
      nextDay(): void {
        const date = store.date();
        if (date !== null) {
          patchState(store, { date: date.add({ days: 1 }) });
        }
      },
      goToToday(): void {
        patchState(store, { date: todayOf(store._clock) });
      },
      openWorkOrder(id: string): void {
        void store._actions.dispatch({ type: WorkOrderActionType.Open, payload: { workOrderId: id } });
      },
    })),
    withHooks({
      onInit(store) {
        store.goToToday();
        let previousToday = todayOf(store._clock);
        effect(() => {
          store.now();
          const today = todayOf(store._clock);
          untracked(() => {
            const shown = store.date();
            const followed = followToday(shown, previousToday, today);
            previousToday = today;
            if (followed !== null && followed !== shown) {
              store.setDate(followed);
            }
          });
        });
      },
    }),
  );
}
