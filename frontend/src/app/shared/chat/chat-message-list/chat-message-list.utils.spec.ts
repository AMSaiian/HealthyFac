import { type ChatMessage, ChatMessageRole, ChatMessageState } from '@shared/chat/chat';
import { PENDING_USER_ID } from '@shared/chat/chat.store.utils';
import {
  anchoredScrollTop,
  deletableMessageIds,
  isPinnedToBottom,
  isScrollKey,
  isUserScroll,
  LOAD_OLDER_THRESHOLD,
  newestUserMessageId,
  PIN_THRESHOLD,
  pinAfterUserInput,
  pinsOnChange,
  shouldFollowGrowth,
  shouldLoadOlder,
} from '@shared/chat/chat-message-list/chat-message-list.utils';

describe('chat-message-list.utils', { tags: ['Chat'] }, () => {
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

  it.each([
    [true, false, true],
    [false, false, false],
    [true, true, false],
  ])('shouldFollowGrowth_WithPinned%sAndAnchoring%s_Returns%s', (isPinned, isAnchoring, expected) => {
    expect(shouldFollowGrowth(isPinned, isAnchoring)).toBe(expected);
  });

  it('pinAfterUserInput_NearTheBottom_Pins', () => {
    expect(pinAfterUserInput({ scrollTop: 460, clientHeight: 225, scrollHeight: 688 })).toBe(true);
  });

  it('pinAfterUserInput_AwayFromTheBottom_Unpins', () => {
    expect(pinAfterUserInput({ scrollTop: 100, clientHeight: 225, scrollHeight: 688 })).toBe(false);
  });

  it('isScrollKey_WithAScrollKeyAndALetter_ReadsBoth', () => {
    expect([isScrollKey('PageUp'), isScrollKey('a')]).toEqual([true, false]);
  });

  it('pinsOnChange_WithANewUserMessage_Pins', () => {
    expect(pinsOnChange({ newestUserId: 'u1', isRunning: false }, { newestUserId: 'u2', isRunning: true })).toBe(true);
  });

  it('pinsOnChange_WithATurnEnd_Pins', () => {
    expect(pinsOnChange({ newestUserId: 'u1', isRunning: true }, { newestUserId: 'u1', isRunning: false })).toBe(true);
  });

  it('pinsOnChange_WithNoNewSendAndNoTurnEnd_DoesNotPin', () => {
    expect(pinsOnChange({ newestUserId: 'u1', isRunning: true }, { newestUserId: 'u1', isRunning: true })).toBe(false);
  });

  it('newestUserMessageId_WithTheNewestFirst_ReturnsTheNewestUserMessage', () => {
    const base = { kind: null, errorCode: null, links: [], drafts: [], text: 't' };
    const messages: ChatMessage[] = [
      { ...base, id: 'a4', sequence: 4, role: ChatMessageRole.Assistant, state: ChatMessageState.Streaming },
      { ...base, id: 'u3', sequence: 3, role: ChatMessageRole.User, state: ChatMessageState.Completed },
      { ...base, id: 'u1', sequence: 1, role: ChatMessageRole.User, state: ChatMessageState.Completed },
    ];

    expect(newestUserMessageId(messages)).toBe('u3');
  });

  it('isUserScroll_ByTheWindowAndThePointer_ReadsBoth', () => {
    expect([isUserScroll(100, 400, false), isUserScroll(500, 400, false), isUserScroll(500, 400, true)]).toEqual([
      true,
      false,
      true,
    ]);
  });
});
