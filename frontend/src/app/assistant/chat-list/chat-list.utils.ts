import type { ChatListItem } from '@shared/chat/chat';
import { skeletonRows } from '@shared/grid/table/grid-display';

/**
 * The rows of the lazy list. The first load shows `min(limit, 10)` empty slots, and a list with a next page ends with
 * one empty slot that the next load fills.
 */
export function listSlots(
  items: readonly ChatListItem[],
  hasMore: boolean,
  isFirstLoad: boolean,
  limit: number,
): (ChatListItem | null)[] {
  if (isFirstLoad) {
    return skeletonRows(limit).map(() => null);
  }
  return hasMore ? [...items, null] : [...items];
}

/** Tells whether the scroller range reaches the end slot of the list, so the next page must load. */
export function needsNextPage(last: number, loadedCount: number): boolean {
  return last >= loadedCount;
}
