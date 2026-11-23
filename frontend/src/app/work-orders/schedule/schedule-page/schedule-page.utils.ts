import type { StagingItem } from '@shared/schedule/staging-area/staging-area.utils';

import { PRIORITY_LABEL_KEYS } from '@work-orders/list/work-order-grid/work-order-grid.utils';
import type { WorkOrderPriority } from '@work-orders/work-order';

/** The width of the backlog drawer in rem. The page keeps the same width free at its right while the drawer is open. */
export const BACKLOG_DRAWER_REM = 20;
export const CONTENT_GUTTER_REM = 1;

/** A backlog work order of the store as a card of the shared staging area: the priority becomes a label and a color. */
export function stagingCardOf(item: {
  readonly id: string;
  readonly title: string;
  readonly estimateMinutes: number;
  readonly priority: WorkOrderPriority;
}): StagingItem {
  return {
    id: item.id,
    title: item.title,
    estimateMinutes: item.estimateMinutes,
    priorityKey: PRIORITY_LABEL_KEYS[item.priority],
    priorityClass: `hf-priority-${item.priority}`,
  };
}

/** The right padding of the page content: the drawer width stays free only for a supervisor with an open drawer. */
export function contentPaddingRemOf(canManage: boolean, isBacklogOpen: boolean): number {
  return canManage && isBacklogOpen ? BACKLOG_DRAWER_REM + CONTENT_GUTTER_REM : CONTENT_GUTTER_REM;
}
