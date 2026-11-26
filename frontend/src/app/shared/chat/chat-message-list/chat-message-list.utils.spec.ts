import { type ChatMessage, ChatMessageRole, ChatMessageState } from '@shared/chat/chat';
import { PENDING_USER_ID } from '@shared/chat/chat.store.utils';
import {
  anchoredScrollTop,
  deletableMessageIds,
  isPinnedToBottom,
  LOAD_OLDER_THRESHOLD,
  PIN_THRESHOLD,
  shouldLoadOlder,
} from '@shared/chat/chat-message-list/chat-message-list.utils';

describe('chat-message-list.utils', () => {
  it('shouldLoadOlder_AtTheThreshold_ReturnsTrue', () => {
    expect(shouldLoadOlder(LOAD_OLDER_THRESHOLD)).toBe(true);
  });

  it('shouldLoadOlder_BelowTheThreshold_ReturnsFalse', () => {
    expect(shouldLoadOlder(LOAD_OLDER_THRESHOLD + 1)).toBe(false);
  });

  it('isPinnedToBottom_AtThePinDistance_ReturnsTrue', () => {
    expect(isPinnedToBottom(1000 - 400 - PIN_THRESHOLD, 400, 1000)).toBe(true);
  });

  it('isPinnedToBottom_FurtherFromTheBottom_ReturnsFalse', () => {
    expect(isPinnedToBottom(1000 - 400 - PIN_THRESHOLD - 1, 400, 1000)).toBe(false);
  });

  it('anchoredScrollTop_AfterOlderMessagesWereAdded_MovesByTheAddedHeight', () => {
    expect(anchoredScrollTop(10, 1000, 1600)).toBe(610);
  });

  it('deletableMessageIds_WithAStreamingReplyAndAPendingMessage_ListsOnlyTheStoredAnsweredUserMessage', () => {
    const base = { kind: null, errorCode: null, links: [], drafts: [], text: 't' };
    const messages: ChatMessage[] = [
      { ...base, id: 'a4', sequence: 4, role: ChatMessageRole.Assistant, state: ChatMessageState.Streaming },
      { ...base, id: 'u3', sequence: 3, role: ChatMessageRole.User, state: ChatMessageState.Completed },
      { ...base, id: 'a2', sequence: 2, role: ChatMessageRole.Assistant, state: ChatMessageState.Completed },
      { ...base, id: 'u1', sequence: 1, role: ChatMessageRole.User, state: ChatMessageState.Completed },
      { ...base, id: PENDING_USER_ID, sequence: 5, role: ChatMessageRole.User, state: ChatMessageState.Completed },
    ];

    expect([...deletableMessageIds(messages)]).toEqual(['u1']);
  });
});
