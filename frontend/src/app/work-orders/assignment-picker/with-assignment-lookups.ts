import { inject, type Signal } from '@angular/core';
import { patchState, signalStoreFeature, type, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, finalize, pipe, switchMap, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { createLookup } from '@shared/lookups/create-lookup';
import { LookupsApi } from '@shared/lookups/lookups-api';

import type { DaysRequest, SpansRequest } from '@work-orders/assignment-picker/assignment-picker';
import type { FreeSpan, ScheduleDay } from '@work-orders/work-order';
import { WorkOrdersApi } from '@work-orders/work-orders-api';

interface AssignmentLookupsState {
  lookupAssigneeId: string | null;
  days: readonly ScheduleDay[];
  isDaysLoading: boolean;
  spans: readonly FreeSpan[] | null;
  isSpansLoading: boolean;
}

/**
 * Merges the days of a new answer into the held days. A day of the answer replaces the held day with the same date.
 */
function mergeDays(held: readonly ScheduleDay[], answered: readonly ScheduleDay[]): readonly ScheduleDay[] {
  const answeredDates = new Set(answered.map((day) => day.date));
  return [...held.filter((day) => !answeredDates.has(day.date)), ...answered];
}

/**
 * The lookups of the assignment picker: the employees, the day summary, and the free spans. The days of the shown
 * months add up while the assignee stays the same; another assignee starts over. A newer request of the same kind
 * replaces an older one (`switchMap`). The host store gives the ID of the edited work order, so its own slot does
 * not count as busy.
 */
export function withAssignmentLookups() {
  return signalStoreFeature(
    { props: type<{ workOrderId: Signal<string | undefined> }>() },
    withState<AssignmentLookupsState>({
      lookupAssigneeId: null,
      days: [],
      isDaysLoading: false,
      spans: null,
      isSpansLoading: false,
    }),
    withProps(() => {
      const lookups = inject(LookupsApi);
      return { assigneeLookup: createLookup((search) => lookups.employees(search)) };
    }),
    withMethods((store) => {
      const api = inject(WorkOrdersApi);
      return {
        loadDays: rxMethod<DaysRequest>(
          pipe(
            switchMap(({ assigneeId, from, to }) => {
              if (assigneeId !== store.lookupAssigneeId()) {
                patchState(store, { lookupAssigneeId: assigneeId, days: [], spans: null });
              }
              patchState(store, { isDaysLoading: true });
              return api.scheduleDays(assigneeId, from.toString(), to.toString(), store.workOrderId()).pipe(
                tap((answer) => patchState(store, { days: mergeDays(store.days(), answer.days) })),
                catchError(() => EMPTY),
                finalize(() => patchState(store, { isDaysLoading: false })),
              );
            }),
          ),
        ),
        loadSpans: rxMethod<SpansRequest>(
          pipe(
            switchMap(({ assigneeId, date }) => {
              patchState(store, { spans: null, isSpansLoading: true });
              return api.freeSpans(assigneeId, date.toString(), store.workOrderId()).pipe(
                tap((answer) => patchState(store, { spans: answer.spans })),
                catchError(() => EMPTY),
                finalize(() => patchState(store, { isSpansLoading: false })),
              );
            }),
          ),
        ),
      };
    }),
  );
}
