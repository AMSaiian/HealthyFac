import { signalStore } from '@ngrx/signals';

import { withActionResultReload } from '@shared/actions/with-action-result-reload';
import { withConfirm } from '@shared/confirm/with-confirm';
import { withToasts } from '@shared/toasts/with-toasts';

import { WorkOrderActionType } from '@work-orders/work-order-actions';

import { RealtimeTopic } from '@shared/realtime/realtime';
import { withRealtimeReload } from '@shared/realtime/with-realtime-reload';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';
import { withScheduleApply } from './schedule.apply.slice';
import { withScheduleDay } from './schedule.day.slice';
import { withSchedulePending } from './schedule.pending.slice';

const RELOAD_RESULT_TYPES: readonly string[] = [WorkOrderActionType.Saved, WorkOrderActionType.Conflict];

/**
 * One day of the schedule tab. A supervisor (`work-orders:manage`) sees every active employee and edits the schedule
 * through pending changes; a worker sees the own day as one row. The route provides it, so it resets per visit.
 */
export const ScheduleStore = signalStore(
  withToasts(),
  withConfirm(),
  withScheduleDay(),
  withSchedulePending(),
  withScheduleApply(),
  withActionResultReload<{ reloadAll: () => void }>(RELOAD_RESULT_TYPES, (store) => {
    store.reloadAll();
  }),
  withRealtimeReload<{ reloadAll: () => void }>([RealtimeTopic.WorkOrders, RealtimeTopic.Schedule], (store) => {
    store.reloadAll();
  }),
  withStoreDevtools('schedule'),
);
