import { ChatActivity, ChatErrorCode, type ChatMessage, ChatMessageKind, ChatMessageState } from '@shared/chat/chat';

const ACTIVITY_KEYS: Readonly<Record<ChatActivity, string>> = {
  [ChatActivity.Machines]: 'CHAT.ACTIVITY.MACHINES',
  [ChatActivity.Signals]: 'CHAT.ACTIVITY.SIGNALS',
  [ChatActivity.Cases]: 'CHAT.ACTIVITY.CASES',
  [ChatActivity.WorkOrders]: 'CHAT.ACTIVITY.WORK_ORDERS',
  [ChatActivity.People]: 'CHAT.ACTIVITY.PEOPLE',
  [ChatActivity.Schedule]: 'CHAT.ACTIVITY.SCHEDULE',
  [ChatActivity.Manuals]: 'CHAT.ACTIVITY.MANUALS',
  [ChatActivity.Files]: 'CHAT.ACTIVITY.FILES',
  [ChatActivity.Drafts]: 'CHAT.ACTIVITY.DRAFTS',
};

const KIND_KEYS: Readonly<Record<ChatMessageKind, string | null>> = {
  [ChatMessageKind.Answer]: null,
  [ChatMessageKind.Refusal]: 'CHAT.KINDS.REFUSAL',
  [ChatMessageKind.Unanswerable]: 'CHAT.KINDS.UNANSWERABLE',
  [ChatMessageKind.BlockedNotice]: 'CHAT.KINDS.BLOCKED_NOTICE',
};

const FAILURE_KEYS: Readonly<Record<ChatErrorCode, string>> = {
  [ChatErrorCode.TimeLimit]: 'CHAT.NOTES.TIME_LIMIT',
  [ChatErrorCode.ProviderUnavailable]: 'CHAT.NOTES.PROVIDER_UNAVAILABLE',
  [ChatErrorCode.OutputLimit]: 'CHAT.NOTES.OUTPUT_LIMIT',
};

/** Tells whether the clamped text hides lines: its content is taller than its box. */
export function isClamped(scrollHeight: number, clientHeight: number): boolean {
  return scrollHeight > clientHeight;
}

/** The translation key of the activity line, or `null` for no activity. */
export function activityKey(activity: ChatActivity | null): string | null {
  return activity === null ? null : ACTIVITY_KEYS[activity];
}

/**
 * The translation key of the note under a message, or `null` for none. A fixed kind (refusal, unanswerable, blocked)
 * replaces the empty text; a cancelled, failed, or interrupted state adds a note under the text.
 */
export function messageNoteKey(message: Pick<ChatMessage, 'kind' | 'state' | 'errorCode'>): string | null {
  if (message.state === ChatMessageState.Cancelled) {
    return 'CHAT.NOTES.CANCELLED';
  }
  if (message.state === ChatMessageState.Interrupted) {
    return 'CHAT.NOTES.INTERRUPTED';
  }
  if (message.state === ChatMessageState.Failed) {
    return message.errorCode === null ? 'CHAT.NOTES.FAILED' : FAILURE_KEYS[message.errorCode];
  }
  return message.kind === null ? null : KIND_KEYS[message.kind];
}
