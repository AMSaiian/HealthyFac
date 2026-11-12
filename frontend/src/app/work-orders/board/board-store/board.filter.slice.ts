import { inject } from '@angular/core';
import { patchState, signalStoreFeature, withHooks, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { debounceTime, pipe, tap } from 'rxjs';

import { CLOCK } from '@shared/time/clock';
import { EMPTY_RANGE, type PlainDateRange } from '@shared/time/plain-date-range';

import { WorkOrdersGridStore } from '@work-orders/list/work-orders-grid.store';
import { TITLE_DEBOUNCE_MS } from './board.store.utils';

interface FilterState {
  titleDraft: string;
  plannedStart: PlainDateRange;
}

/** The own filter of the board: the title box and the planned start range, which defaults to today. */
export function withBoardFilter() {
  return signalStoreFeature(
    withState<FilterState>({ titleDraft: '', plannedStart: EMPTY_RANGE }),
    withMethods((store) => {
      const grid = inject(WorkOrdersGridStore);

      const pushTitle = rxMethod<string>(
        pipe(
          debounceTime(TITLE_DEBOUNCE_MS),
          tap((title) => grid.setFilter({ title })),
        ),
      );

      return {
        /** Keeps the text of the title box and writes it to the shared filter after a pause, so a key press reloads nothing. */
        setTitle(title: string): void {
          patchState(store, { titleDraft: title });
          pushTitle(title);
        },
        setPlannedStart(plannedStart: PlainDateRange): void {
          patchState(store, { plannedStart });
        },
      };
    }),
    withHooks({
      onInit(store) {
        const clock = inject(CLOCK);
        const today = clock.now().toZonedDateTimeISO(clock.timeZoneId()).toPlainDate();
        store.setPlannedStart({ from: today, to: today });
      },
    }),
  );
}
