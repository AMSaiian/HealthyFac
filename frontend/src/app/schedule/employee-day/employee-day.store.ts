import { computed, effect, inject, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { TranslateService } from '@ngx-translate/core';
import { type Observable, of, tap } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import { type Reloadable, withActionResultReload } from '@shared/actions/with-action-result-reload';
import { EmployeeStore } from '@shared/employee/employee.store';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import type { ScheduleRow } from '@shared/schedule/schedule-row';
import { CLOCK } from '@shared/time/clock';
import type { Temporal } from '@shared/time/temporal';

import type { DaySchedule } from '@work-orders/work-order';
import { WorkOrderActionType } from '@work-orders/work-order-actions';
import { ScheduleApi } from '@schedule/schedule-api';
import { followToday, minuteTicker, todayOf, toRow } from '@schedule/schedule-time';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

const RELOAD_RESULT_TYPES: readonly string[] = [WorkOrderActionType.Saved, WorkOrderActionType.Conflict];
const FALLBACK_LABEL_KEY = 'SCHEDULE.EMPLOYEE';

interface EmployeeDayState {
  employeeId: string | null;
  date: Temporal.PlainDate | null;
}

interface PreloadedDay {
  employeeId: string;
  date: Temporal.PlainDate;
  day: DaySchedule;
}

/** One day of one employee. The route provides it, and the route resolver opens the first day. */
export const EmployeeDayStore = signalStore(
  withState<EmployeeDayState>({ employeeId: null, date: null }),
  withProps(() => ({
    _api: inject(ScheduleApi),
    _clock: inject(CLOCK),
    _actions: inject(ActionStore),
    _auth: inject(AuthStore),
    _employee: inject(EmployeeStore),
    _translate: inject(TranslateService),
    _preloaded: { value: null as PreloadedDay | null },
  })),
  withProps((store) => ({
    now: minuteTicker(store._clock),
    day: rxResource({
      params: () => {
        const employeeId = store.employeeId();
        const date = store.date();
        return employeeId === null || date === null ? undefined : { employeeId, date };
      },
      stream: ({ params }) => {
        const preloaded = store._preloaded.value;
        store._preloaded.value = null;
        return preloaded !== null && preloaded.employeeId === params.employeeId && preloaded.date.equals(params.date)
          ? of(preloaded.day)
          : store._api.employeeDay(params.employeeId, params.date.toString());
      },
    }),
  })),
  withComputed((store) => ({
    hasFailed: computed(() => store.day.status() === 'error'),
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
    /**
     * Requests the day of today for the route resolver, keeps it, and then sets the employee and the date. The day
     * resource takes the kept day instead of asking again, so the first day is requested once.
     */
    openDay(employeeId: string): Observable<DaySchedule> {
      const date = todayOf(store._clock);
      return store._api.employeeDay(employeeId, date.toString()).pipe(
        tap((day) => {
          store._preloaded.value = { employeeId, date, day };
          patchState(store, { employeeId, date });
        }),
      );
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
  withActionResultReload<{ day: Reloadable }>(RELOAD_RESULT_TYPES, (store) => {
    store.day.reload();
  }),
  withStoreDevtools('employee-day'),
);
