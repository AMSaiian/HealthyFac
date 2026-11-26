import { type ChatMessage, ChatMessageRole, ChatMessageState } from '@shared/chat/chat';
import { PENDING_USER_ID } from '@shared/chat/chat.store.utils';

/** The distance from the top in pixels that starts the load of an older page. */
export const LOAD_OLDER_THRESHOLD = 80;

/** The distance from the bottom in pixels that still counts as pinned to the bottom. */
export const PIN_THRESHOLD = 40;

/** Tells whether the scroll position is near the top, so the older page must load. */
export function shouldLoadOlder(scrollTop: number): boolean {
  return scrollTop <= LOAD_OLDER_THRESHOLD;
}

/** Tells whether the view sits at the bottom (within the pin threshold). */
export function isPinnedToBottom(scrollTop: number, clientHeight: number, scrollHeight: number): boolean {
  return scrollHeight - scrollTop - clientHeight <= PIN_THRESHOLD;
}

/** The scroll position that keeps the message on top in place after older messages were added above it. */
export function anchoredScrollTop(scrollTop: number, heightBefore: number, heightAfter: number): number {
  return scrollTop + (heightAfter - heightBefore);
}

/** The IDs of the stored user messages whose reply (the next sequence) is not streaming. */
export function deletableMessageIds(messages: readonly ChatMessage[]): ReadonlySet<string> {
  const streamingSequences = new Set(
    messages.filter((message) => message.state === ChatMessageState.Streaming).map((message) => message.sequence),
  );
  return new Set(
    messages
      .filter(
        (message) =>
          message.role === ChatMessageRole.User &&
          message.id !== PENDING_USER_ID &&
          !streamingSequences.has(message.sequence + 1),
      )
      .map((message) => message.id),
  );
}
