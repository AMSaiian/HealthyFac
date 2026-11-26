import type { ChatActivity, ChatMessage, ChatMessagesPage, ChatTurnUpdate } from '@shared/chat/chat';

/** The number of messages that one page of a chat loads. */
export const CHAT_MESSAGES_PAGE_SIZE = 30;

/** The turn of a chat that streams now, as the push shows it. */
export interface RunningTurn {
  readonly messageId: string;
  readonly text: string;
  readonly sequence: number;
  readonly activity: ChatActivity | null;
}

/** Tells whether a push is older than the push that the store already applied for the same reply. */
export function isStaleUpdate(running: RunningTurn | undefined, update: ChatTurnUpdate): boolean {
  return running !== undefined && running.messageId === update.messageId && update.sequence <= running.sequence;
}

/**
 * Merges the newest page into the loaded messages (newest first): a message with the same ID is replaced, a new one
 * goes on top, and the older pages stay loaded. `nextBefore` stays when an older page is loaded, else it comes from the
 * page.
 */
export function mergeNewestPage(
  loaded: readonly ChatMessage[],
  page: ChatMessagesPage,
  nextBefore: number | null,
): { messages: readonly ChatMessage[]; nextBefore: number | null } {
  const pageIds = new Set(page.items.map((message) => message.id));
  const oldestInPage = Math.min(...page.items.map((message) => message.sequence));
  const hasOlderLoaded = page.items.length > 0 && loaded.some((message) => message.sequence < oldestInPage);
  return {
    messages: [...page.items, ...loaded.filter((message) => !pageIds.has(message.id))],
    nextBefore: hasOlderLoaded ? nextBefore : page.nextBefore,
  };
}

/** Adds an older page after the loaded messages (newest first) and drops a message that is loaded already. */
export function appendOlderPage(loaded: readonly ChatMessage[], page: ChatMessagesPage): readonly ChatMessage[] {
  const loadedIds = new Set(loaded.map((message) => message.id));
  return [...loaded, ...page.items.filter((message) => !loadedIds.has(message.id))];
}
