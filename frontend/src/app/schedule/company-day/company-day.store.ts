import { computed, effect, inject, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';

import { ActionStore } from '@shared/actions/action.store';
import type { ScheduleRow } from '@shared/schedule/schedule-row';
import { CLOCK } from '@shared/time/clock';
import type { Temporal } from '@shared/time/temporal';

import { WorkOrderActionType } from '@work-orders/work-order-actions';
import { ScheduleApi } from '@schedule/schedule-api';
import { followToday, minuteTicker, todayOf, toRow } from '@schedule/schedule-time';

const RELOAD_RESULT_TYPES: readonly string[] = [WorkOrderActionType.Saved, WorkOrderActionType.Conflict];

interface CompanyDayState {
  date: Temporal.PlainDate | null;
}

/** One day of every active employee of the company. The route provides it. */
export const CompanyDayStore = signalStore(
  withState<CompanyDayState>({ date: null }),
  withProps(() => ({
    _api: inject(ScheduleApi),
    _clock: inject(CLOCK),
    _actions: inject(ActionStore),
  })),
  withProps((store) => ({
    now: minuteTicker(store._clock),
    day: rxResource({
      params: () => store.date() ?? undefined,
      stream: ({ params }) => store._api.companyDay(params.toString()),
    }),
  })),
  withComputed((store) => ({
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
