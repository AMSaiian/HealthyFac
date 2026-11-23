import type { StagingItem } from '@shared/schedule/staging-area/staging-area.utils';

import { PRIORITY_LABEL_KEYS } from '@work-orders/list/work-order-grid/work-order-grid.utils';
import type { WorkOrderPriority } from '@work-orders/work-order';

/** The width of the backlog drawer in rem. It opens over the board. */
export const BACKLOG_DRAWER_REM = 20;

/** A backlog work order of the store as a chip of the shared staging area: the priority becomes a label and a color. */
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

/**
 * The state of the backlog drawer. It is mounted while it is open, and also while a drag from the backlog runs, so the
 * dragged chip stays in the page until the drag ends (the CDK ends a drag whose source leaves the page). A drag that
 * runs while the drawer is closed keeps it mounted but hidden.
 */
export function backlogDrawerStateOf(
  isOpen: boolean,
  isBacklogDragActive: boolean,
): { readonly isMounted: boolean; readonly isHidden: boolean } {
  return { isMounted: isOpen || isBacklogDragActive, isHidden: !isOpen && isBacklogDragActive };
}

export const BACKLOG_BUTTON_TEXT_KEY = 'WORK_ORDERS.SCHEDULE_TAB.BACKLOG_TOGGLE';
export const BACKLOG_DROP_TEXT_KEY = 'WORK_ORDERS.SCHEDULE_TAB.DROP_TO_BACKLOG';

/**
 * The backlog button of the toolbar. While a bar of the board drags, the button is the target that returns the work
 * order to the backlog: it shows the drop text and the highlight. Otherwise it shows the count and toggles the drawer.
 */
export function backlogButtonOf(isBoardDragActive: boolean): {
  readonly textKey: string;
  readonly isHighlighted: boolean;
} {
  return isBoardDragActive
    ? { textKey: BACKLOG_DROP_TEXT_KEY, isHighlighted: true }
    : { textKey: BACKLOG_BUTTON_TEXT_KEY, isHighlighted: false };
}

export type BacklogDragEvent = 'backlogDragStarted' | 'boardDragStarted' | 'boardDragEnded';

/** The open state of the backlog drawer after a drag event: a drag start closes it, and the end of a board drag keeps it. */
export function isBacklogOpenAfterDrag(isOpen: boolean, event: BacklogDragEvent): boolean {
  return event === 'boardDragEnded' ? isOpen : false;
}
