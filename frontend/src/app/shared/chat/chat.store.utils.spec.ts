import { type ChatMessage, ChatMessageRole, ChatMessageState } from '@shared/chat/chat';
import { appendOlderPage, isStaleUpdate, mergeNewestPage } from '@shared/chat/chat.store.utils';

function message(id: string, sequence: number): ChatMessage {
  return {
    id,
    sequence,
    role: ChatMessageRole.User,
    kind: null,
    state: ChatMessageState.Completed,
    text: id,
    errorCode: null,
    links: [],
    drafts: [],
  };
}

describe('chat.store.utils', () => {
  it('isStaleUpdate_WithAnEqualSequenceOfTheSameReply_ReturnsTrue', () => {
    const running = { messageId: 'm1', text: 'a', sequence: 5, activity: null };

    const result = isStaleUpdate(running, { chatId: 'c1', messageId: 'm1', sequence: 5, text: 'b', activity: null });

    expect(result).toBe(true);
  });

  it('isStaleUpdate_WithALowerSequenceOfTheSameReply_ReturnsTrue', () => {
    const running = { messageId: 'm1', text: 'a', sequence: 5, activity: null };

    const result = isStaleUpdate(running, { chatId: 'c1', messageId: 'm1', sequence: 4, text: 'b', activity: null });

    expect(result).toBe(true);
  });

  it('isStaleUpdate_WithALowerSequenceOfAnotherReply_ReturnsFalse', () => {
    const running = { messageId: 'm1', text: 'a', sequence: 5, activity: null };

    const result = isStaleUpdate(running, { chatId: 'c1', messageId: 'm2', sequence: 1, text: 'b', activity: null });

    expect(result).toBe(false);
  });

  it('appendOlderPage_WithAMessageThatIsLoaded_DropsTheDuplicate', () => {
    const loaded = [message('m3', 3), message('m2', 2)];

    const result = appendOlderPage(loaded, { items: [message('m2', 2), message('m1', 1)], nextBefore: null });

    expect(result.map((item) => item.id)).toEqual(['m3', 'm2', 'm1']);
  });

  it('mergeNewestPage_WithAnOlderPageLoaded_KeepsItAndTheNextBefore', () => {
    const loaded = [message('m4', 4), message('m3', 3), message('m2', 2)];

    const result = mergeNewestPage(loaded, { items: [message('m5', 5), message('m4', 4)], nextBefore: 4 }, 2);

    expect(result.messages.map((item) => item.id)).toEqual(['m5', 'm4', 'm3', 'm2']);
    expect(result.nextBefore).toBe(2);
  });

  it('mergeNewestPage_WithOnlyTheNewestPageLoaded_TakesTheNextBeforeOfThePage', () => {
    const result = mergeNewestPage(
      [message('m2', 2)],
      { items: [message('m3', 3), message('m2', 2)], nextBefore: null },
      2,
    );

    expect(result.nextBefore).toBeNull();
  });
});
