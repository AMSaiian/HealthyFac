import { computed, effect, inject, type Signal, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import {
  patchState,
  signalStoreFeature,
  type,
  withComputed,
  withHooks,
  withMethods,
  withProps,
  withState,
} from '@ngrx/signals';

import type { HistoryStore } from '@shared/history/history.store';
import type { Temporal } from '@shared/time/temporal';

import type { ShiftBlock } from '@employees/shifts/shift';
import { cutSeconds, isOverrideDateTaken, type ShiftDialog } from '@employees/shifts/shifts-store/shifts.store.utils';
import { WorkOrdersApi } from '@work-orders/work-orders-api';

interface PrefillState {
  /** The date that the open new override form shows, or `null` for the default date (tomorrow). */
  prefillDate: Temporal.PlainDate | null;
}

/**
 * The blocks of the day that a new override starts with. The blocks come from the day schedule of the chosen date
 * (the template, or an override of that date). The date follows the form: each date change loads the blocks again.
 */
export function withShiftsPrefill() {
  return signalStoreFeature(
    {
      state: type<{ employeeId: string | null }>(),
      props: type<{
        today: Signal<Temporal.PlainDate>;
        dialog: Signal<ShiftDialog | null>;
        _history: InstanceType<typeof HistoryStore>;
      }>(),
    },
    withState<PrefillState>({ prefillDate: null }),
    withProps((store) => {
      const api = inject(WorkOrdersApi);
      return {
        _prefill: rxResource({
          params: () => {
            const employeeId = store.employeeId();
            const dialog = store.dialog();
            if (employeeId === null || dialog?.kind !== 'override' || dialog.override !== null) {
              return undefined;
            }
            return { employeeId, date: store.prefillDate() ?? store.today().add({ days: 1 }) };
          },
          stream: ({ params }) => api.daySchedule(params.employeeId, params.date.toString()),
        }),
      };
    }),
    withComputed((store) => {
      const isDateTaken = computed(() => {
        const dialog = store.dialog();
        return (
          dialog?.kind === 'override' &&
          isOverrideDateTaken(
            dialog.override === null,
            store._prefill.hasValue() ? store._prefill.value().source : null,
          )
        );
      });
      return {
        isPrefillLoading: computed(() => store._prefill.isLoading()),
        /** Whether the chosen date of a new override already has an override. The dialog tells the user to edit it. */
        isDateTaken,
        prefillBlocks: computed((): readonly ShiftBlock[] | null => {
          if (!store._prefill.hasValue()) {
            return null;
          }
          return isDateTaken()
            ? []
            : store._prefill
                .value()
                .blocks.map((block) => ({ start: cutSeconds(block.startLocal), end: cutSeconds(block.endLocal) }));
        }),
      };
    }),
    withMethods((store) => ({
      setPrefillDate(date: Temporal.PlainDate | null): void {
        const current = store.prefillDate() ?? store.today().add({ days: 1 });
        if (date !== null && !date.equals(current)) {
          patchState(store, { prefillDate: date });
        }
      },
    })),
    withHooks({
      onInit(store) {
        effect(() => {
          store._history.compose();
          untracked(() => patchState(store, { prefillDate: null }));
        });
      },
    }),
  );
}
