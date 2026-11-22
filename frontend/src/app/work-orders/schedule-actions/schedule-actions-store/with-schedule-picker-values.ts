import { computed, type Signal } from '@angular/core';
import type { ResourceRef } from '@angular/core';
import { signalStoreFeature, type, withComputed } from '@ngrx/signals';

import type { Lookup } from '@shared/lookups/create-lookup';
import { type LookupOption, mergeLookupOptions } from '@shared/lookups/lookup-options';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';
import { inject } from '@angular/core';

import type { AssignmentValue } from '@work-orders/assignment-picker/assignment-picker.utils';
import type { WorkOrder } from '@work-orders/work-order';
import { WorkOrderActionType } from '@work-orders/work-order-actions';

/**
 * The values that the assignment picker of the assign and move dialogs reads: the start values, the options of the
 * assignee select, the estimate of the work order, and today.
 */
export function withSchedulePickerValues() {
  return signalStoreFeature(
    {
      props: type<{
        type: Signal<string | null>;
        isScheduleDialog: Signal<boolean>;
        workOrder: ResourceRef<WorkOrder | undefined>;
        assigneeLookup: Lookup;
      }>(),
    },
    withComputed((store) => {
      const clock = inject(CLOCK);
      return {
        estimateMinutes: computed(() => (store.workOrder.hasValue() ? store.workOrder.value().estimateMinutes : null)),
        today: computed(() => clock.now().toZonedDateTimeISO(clock.timeZoneId()).toPlainDate()),
        /** A move starts at the stored slot, an assign with nothing chosen. */
        initialAssignValue: computed((): AssignmentValue | null => {
          if (!store.isScheduleDialog() || !store.workOrder.hasValue()) {
            return null;
          }
          const workOrder = store.workOrder.value();
          const isMove = store.type() === WorkOrderActionType.Move && workOrder.plannedStartLocal !== null;
          return {
            assigneeId: isMove ? workOrder.assigneeId : null,
            start:
              isMove && workOrder.plannedStartLocal !== null
                ? Temporal.PlainDateTime.from(workOrder.plannedStartLocal)
                : null,
          };
        }),
        employeeOptions: computed((): LookupOption[] => {
          const current = store.workOrder.hasValue() ? store.workOrder.value() : null;
          return mergeLookupOptions(
            store.type() === WorkOrderActionType.Move && current?.assigneeId != null
              ? [{ id: current.assigneeId, displayName: current.assigneeName ?? current.assigneeId, isDeleted: false }]
              : [],
            store.assigneeLookup.options(),
          );
        }),
      };
    }),
  );
}
