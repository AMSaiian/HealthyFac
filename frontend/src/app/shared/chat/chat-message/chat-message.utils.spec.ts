import {
  ChatActivity,
  type ChatDraft,
  ChatDraftKind,
  ChatErrorCode,
  ChatMessageKind,
  ChatMessageState,
} from '@shared/chat/chat';
import { activityKey, chatDraftJumpOf, isClamped, messageNoteKey } from '@shared/chat/chat-message/chat-message.utils';

describe('chat-message.utils', { tags: ['Chat'] }, () => {
  it('isClamped_WithMoreContentThanBox_ReturnsTrue', () => {
    expect(isClamped(73, 72)).toBe(true);
  });

  it('isClamped_WithContentEqualToBox_ReturnsFalse', () => {
    expect(isClamped(72, 72)).toBe(false);
  });

  it('activityKey_WithNoActivity_ReturnsNull', () => {
    expect(activityKey(null)).toBeNull();
  });

  it('activityKey_WithGeneral_ReturnsItsKey', () => {
    expect(activityKey(ChatActivity.General)).toBe('CHAT.ACTIVITY.GENERAL');
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

  it('chatDraftJumpOf_WithACommittedWorkOrderDraft_OpensTheWorkOrderDraftsWithTheDraft', () => {
    const draft: ChatDraft = {
      kind: ChatDraftKind.WorkOrder,
      draftId: 'd1',
      title: 'Seal',
      targetMachineId: null,
      isCommitted: true,
    };

    expect(chatDraftJumpOf(draft, 'c1')).toEqual({
      type: 'route',
      commands: ['/work-orders/drafts'],
      queryParams: { draftId: 'd1' },
    });
  });

  it('chatDraftJumpOf_WithACommittedMetricDraft_OpensTheMachineDraftsOfTheChat', () => {
    const draft: ChatDraft = {
      kind: ChatDraftKind.Metric,
      draftId: 'd2',
      title: 'Bearing',
      targetMachineId: 'm1',
      isCommitted: true,
    };

    expect(chatDraftJumpOf(draft, 'c1')).toEqual({
      type: 'action',
      action: { type: 'machines.drafts', payload: { chatId: 'c1' } },
    });
  });

  it('chatDraftJumpOf_WithAnUncommittedDraft_ReturnsNull', () => {
    const draft: ChatDraft = {
      kind: ChatDraftKind.Machine,
      draftId: 'd3',
      title: 'Pump',
      targetMachineId: null,
      isCommitted: false,
    };

    expect(chatDraftJumpOf(draft, 'c1')).toBeNull();
  });
});
