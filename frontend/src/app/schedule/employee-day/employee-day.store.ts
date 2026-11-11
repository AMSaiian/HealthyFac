import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, effect, inject, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { TranslateService } from '@ngx-translate/core';

import { ActionStore } from '@shared/actions/action.store';
import { EmployeeStore } from '@shared/employee/employee.store';
import { AuthStore } from '@shared/security/auth.store';
import { Permission } from '@shared/security/permissions';
import type { ScheduleRow } from '@shared/schedule/schedule-row';
import { CLOCK } from '@shared/time/clock';
import type { Temporal } from '@shared/time/temporal';

import { WorkOrderActionType } from '@work-orders/work-order-actions';
import { ScheduleApi } from '@schedule/schedule-api';
import { followToday, minuteTicker, todayOf, toRow } from '@schedule/schedule-time';

const RELOAD_RESULT_TYPES: readonly string[] = [WorkOrderActionType.Saved, WorkOrderActionType.Conflict];
const FALLBACK_LABEL_KEY = 'SCHEDULE.EMPLOYEE';

interface EmployeeDayState {
  employeeId: string | null;
  date: Temporal.PlainDate | null;
}

/** One day of one employee. The route provides it; the page passes the employee ID of the route. */
export const EmployeeDayStore = signalStore(
  withState<EmployeeDayState>({ employeeId: null, date: null }),
  withProps(() => ({
    _api: inject(ScheduleApi),
    _clock: inject(CLOCK),
    _actions: inject(ActionStore),
    _auth: inject(AuthStore),
    _employee: inject(EmployeeStore),
    _translate: inject(TranslateService),
  })),
  withProps((store) => ({
    now: minuteTicker(store._clock),
    day: rxResource({
      params: () => {
        const employeeId = store.employeeId();
        const date = store.date();
        return employeeId === null || date === null ? undefined : { employeeId, date };
      },
      stream: ({ params }) => store._api.employeeDay(params.employeeId, params.date.toString()),
    }),
  })),
  withComputed((store) => ({
    canGoBack: computed(() => store._auth.hasPermission(Permission.EmployeesRead)),
    isNotFound: computed(() => {
      const error: unknown = store.day.error();
      return error instanceof HttpErrorResponse && error.status === HttpStatusCode.NotFound;
    }),
    rows: computed((): readonly ScheduleRow[] => {
      const employeeId = store.employeeId();
      if (employeeId === null || !store.day.hasValue()) {
        return [];
      }
      const label =
        store._employee.id() === employeeId
          ? (store._employee.name() ?? '')
          : store._translate.instant(FALLBACK_LABEL_KEY);
      return [toRow(employeeId, label, store.day.value())];
    }),
  })),
  withMethods((store) => ({
    setEmployee(employeeId: string): void {
      patchState(store, { employeeId });
    },
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
      let seen = store._actions.lastResult()?.sequence ?? 0;
      effect(() => {
        const result = store._actions.lastResult();
        if (result !== null && RELOAD_RESULT_TYPES.includes(result.type) && result.sequence > seen) {
          seen = result.sequence;
          untracked(() => store.day.reload());
        }
      });
    },
  }),
);
