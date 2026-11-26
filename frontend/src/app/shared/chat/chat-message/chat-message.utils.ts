import { MachineActionType } from '@machines/machine-actions';
import { DRAFT_ID_QUERY_PARAM, WORK_ORDER_DRAFTS_PATH } from '@work-orders/work-order-links';
import {
  ChatActivity,
  type ChatDraft,
  ChatDraftKind,
  ChatErrorCode,
  type ChatMessage,
  ChatMessageKind,
  ChatMessageState,
} from '@shared/chat/chat';
import type { NotificationJump } from '@shared/notifications/notification.store.utils';

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

/** How each kind of draft opens its review: the work order drafts page, or the machine drafts action of the chat. */
const DRAFT_JUMPS: Readonly<Record<ChatDraftKind, (draft: ChatDraft, chatId: string) => NotificationJump>> = {
  [ChatDraftKind.WorkOrder]: (draft) => ({
    type: 'route',
    commands: [WORK_ORDER_DRAFTS_PATH],
    queryParams: { [DRAFT_ID_QUERY_PARAM]: draft.draftId },
  }),
  [ChatDraftKind.Machine]: (_draft, chatId) => machineDraftsOfChat(chatId),
  [ChatDraftKind.Metric]: (_draft, chatId) => machineDraftsOfChat(chatId),
  [ChatDraftKind.Status]: (_draft, chatId) => machineDraftsOfChat(chatId),
  [ChatDraftKind.OperatingPhase]: (_draft, chatId) => machineDraftsOfChat(chatId),
};

function machineDraftsOfChat(chatId: string): NotificationJump {
  return { type: 'action', action: { type: MachineActionType.Drafts, payload: { chatId } } };
}

/** The kinds that have a jump. A test compares them with the kinds of the backend. */
export const CHAT_DRAFT_JUMP_KINDS: readonly ChatDraftKind[] = Object.keys(DRAFT_JUMPS) as ChatDraftKind[];

/** Where the jump of a draft chip goes, or `null` while the draft is not created (the turn did not complete). */
export function chatDraftJumpOf(draft: ChatDraft, chatId: string): NotificationJump | null {
  return draft.isCommitted ? DRAFT_JUMPS[draft.kind](draft, chatId) : null;
}
