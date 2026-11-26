import { ChatActivity, ChatErrorCode, ChatMessageKind, ChatMessageState } from '@shared/chat/chat';
import { activityKey, isClamped, messageNoteKey } from '@shared/chat/chat-message/chat-message.utils';

describe('chat-message.utils', () => {
  it('isClamped_WithMoreContentThanBox_ReturnsTrue', () => {
    expect(isClamped(73, 72)).toBe(true);
  });

  it('isClamped_WithContentEqualToBox_ReturnsFalse', () => {
    expect(isClamped(72, 72)).toBe(false);
  });

  it('activityKey_WithNoActivity_ReturnsNull', () => {
    expect(activityKey(null)).toBeNull();
  });

  it('activityKey_WithWorkOrders_ReturnsItsKey', () => {
    expect(activityKey(ChatActivity.WorkOrders)).toBe('CHAT.ACTIVITY.WORK_ORDERS');
  });

  it('messageNoteKey_WithAFailedStateAndAnOutputLimit_ReturnsTheCutKey', () => {
    const key = messageNoteKey({
      kind: null,
      state: ChatMessageState.Failed,
      errorCode: ChatErrorCode.OutputLimit,
    });

    expect(key).toBe('CHAT.NOTES.OUTPUT_LIMIT');
  });

  it('messageNoteKey_WithACancelledState_ReturnsTheCancelledKey', () => {
    const key = messageNoteKey({ kind: null, state: ChatMessageState.Cancelled, errorCode: null });

    expect(key).toBe('CHAT.NOTES.CANCELLED');
  });

  it('messageNoteKey_WithACompletedRefusal_ReturnsTheRefusalKey', () => {
    const key = messageNoteKey({
      kind: ChatMessageKind.Refusal,
      state: ChatMessageState.Completed,
      errorCode: null,
    });

    expect(key).toBe('CHAT.KINDS.REFUSAL');
  });

  it('messageNoteKey_WithACompletedAnswer_ReturnsNull', () => {
    const key = messageNoteKey({
      kind: ChatMessageKind.Answer,
      state: ChatMessageState.Completed,
      errorCode: null,
    });

    expect(key).toBeNull();
  });
});
