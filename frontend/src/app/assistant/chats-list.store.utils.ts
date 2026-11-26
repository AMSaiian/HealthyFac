/** The number of chats that one page of the list loads. */
export const CHATS_PAGE_SIZE = 20;

/**
 * Builds the rule that decides when the chat list reloads. The list reloads once when a turn ended (the ended count
 * changed) or when a new chat was deleted, so the title, the order, and the removed chat show.
 * @param initialEndedCount The ended count when the host opens.
 * @param initialDeletedChatId The deleted chat ID when the host opens.
 * @returns A function that takes the current values and tells whether the list must reload.
 */
export function createListReloadRule(
  initialEndedCount: number,
  initialDeletedChatId: string | null,
): (endedCount: number, deletedChatId: string | null) => boolean {
  let seenEndedCount = initialEndedCount;
  let seenDeletedChatId = initialDeletedChatId;

  return (endedCount, deletedChatId) => {
    const isTurnEnded = endedCount !== seenEndedCount;
    const isChatDeleted = deletedChatId !== null && deletedChatId !== seenDeletedChatId;
    seenEndedCount = endedCount;
    seenDeletedChatId = deletedChatId;

    return isTurnEnded || isChatDeleted;
  };
}
