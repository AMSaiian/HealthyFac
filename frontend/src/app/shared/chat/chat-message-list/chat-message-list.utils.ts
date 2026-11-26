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

/** Tells whether a growth of the content scrolls the view to the bottom: only while it is pinned and anchors nothing. */
export function shouldFollowGrowth(isPinned: boolean, isAnchoring: boolean): boolean {
  return isPinned && !isAnchoring;
}

/** The pin state after the user scrolled: pinned when the view sits at the bottom, else unpinned. */
export function pinAfterUserInput(position: {
  readonly scrollTop: number;
  readonly clientHeight: number;
  readonly scrollHeight: number;
}): boolean {
  return isPinnedToBottom(position.scrollTop, position.clientHeight, position.scrollHeight);
}

/**
 * Tells whether a scroll event comes from the user: inside the short window after a wheel, key, or touch input, or for
 * as long as a pointer stays pressed on the scroller (a drag of the scroll bar sends one pointer down and then scroll
 * events until the pointer is released).
 */
export function isUserScroll(now: number, windowEnd: number, isPointerHeld: boolean): boolean {
  return isPointerHeld || now < windowEnd;
}

const SCROLL_KEYS: ReadonlySet<string> = new Set(['ArrowUp', 'ArrowDown', 'PageUp', 'PageDown', 'Home', 'End', ' ']);

/** Tells whether a key scrolls the view. */
export function isScrollKey(key: string): boolean {
  return SCROLL_KEYS.has(key);
}

/** What the view watches to pin itself: the newest user message and whether a turn runs. */
export interface PinWatch {
  readonly newestUserId: string | null;
  readonly isRunning: boolean;
}

/** Tells whether the view pins itself: after the user's own send (a new newest user message) and at a turn end. */
export function pinsOnChange(previous: PinWatch, current: PinWatch): boolean {
  const isNewSend = current.newestUserId !== null && current.newestUserId !== previous.newestUserId;
  return isNewSend || (previous.isRunning && !current.isRunning);
}

/** The newest user message of the loaded messages (the newest first), or `null`. */
export function newestUserMessageId(messages: readonly ChatMessage[]): string | null {
  return messages.find((message) => message.role === ChatMessageRole.User)?.id ?? null;
}
