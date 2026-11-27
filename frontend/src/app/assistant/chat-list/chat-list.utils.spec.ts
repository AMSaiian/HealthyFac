import { listSlots, needsNextPage } from '@assistant/chat-list/chat-list.utils';
import { type ChatListItem, ChatState } from '@shared/chat/chat';
import { MAX_SKELETON_ROWS } from '@shared/grid/table/grid-display';

function item(id: string): ChatListItem {
  return { id, title: id, state: ChatState.Open, updatedAt: '2026-01-01T00:00:00Z' };
}

describe('listSlots', { tags: ['Chat'] }, () => {
  it('listSlots_OnTheFirstLoad_ReturnsAtMostTenEmptySlots', () => {
    const slots = listSlots([], false, true, 50);

    expect(slots).toHaveLength(MAX_SKELETON_ROWS);
    expect(slots.every((slot) => slot === null)).toBe(true);
  });

  it('listSlots_WithAShortLimitOnTheFirstLoad_ReturnsTheLimit', () => {
    const slots = listSlots([], false, true, 3);

    expect(slots).toHaveLength(3);
  });

  it('listSlots_WithANextPage_EndsWithOneEmptySlot', () => {
    const slots = listSlots([item('a')], true, false, 20);

    expect(slots).toEqual([item('a'), null]);
  });

  it('listSlots_AtTheLastPage_HasNoEmptySlot', () => {
    const slots = listSlots([item('a')], false, false, 20);

    expect(slots).toEqual([item('a')]);
  });
});

describe('needsNextPage', { tags: ['Chat'] }, () => {
  it('needsNextPage_WhenTheRangeReachesTheEndSlot_ReturnsTrue', () => {
    expect(needsNextPage(5, 5)).toBe(true);
  });

  it('needsNextPage_WhenTheRangeStaysInTheLoadedRows_ReturnsFalse', () => {
    expect(needsNextPage(4, 5)).toBe(false);
  });
});
