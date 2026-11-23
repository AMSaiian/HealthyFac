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
 * runs while the drawer is closed keeps it mounted but hidden. After such a drag the drawer stays hidden while it closes
 * (`isClosingQuietly`), so no closing motion shows. A close by the button or by a click outside keeps its motion.
 */
export function backlogDrawerStateOf(
  isOpen: boolean,
  isBacklogDragActive: boolean,
  isClosingQuietly: boolean,
): { readonly isMounted: boolean; readonly isHidden: boolean } {
  return { isMounted: isOpen || isBacklogDragActive, isHidden: !isOpen && (isBacklogDragActive || isClosingQuietly) };
}

/** The band over the toolbar row that returns a work order to the backlog: shown while a bar of the board drags. */
export function isReturnBandShown(isBoardDragActive: boolean, canManage: boolean): boolean {
  return isBoardDragActive && canManage;
}

export type BacklogDragEvent = 'backlogDragStarted' | 'boardDragStarted' | 'boardDragEnded';

/** The open state of the backlog drawer after a drag event: a drag start closes it, and the end of a board drag keeps it. */
export function isBacklogOpenAfterDrag(isOpen: boolean, event: BacklogDragEvent): boolean {
  return event === 'boardDragEnded' ? isOpen : false;
}

export const BACKLOG_DRAWER_CLASS = 'hf-backlog-drawer';
export const BACKLOG_TOGGLE_ATTRIBUTE = 'data-backlog-toggle';

/**
 * What a pointer down does to the open backlog drawer: inside the drawer it stays, on the toggle button the button
 * toggles it, and anywhere else it closes the drawer.
 */
export function backlogPointerActionOf(isInsideDrawer: boolean, isOnToggle: boolean): 'stay' | 'toggle' | 'close' {
  if (isInsideDrawer) {
    return 'stay';
  }
  return isOnToggle ? 'toggle' : 'close';
}
