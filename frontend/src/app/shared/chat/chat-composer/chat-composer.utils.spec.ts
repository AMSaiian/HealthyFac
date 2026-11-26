import { CHAT_TEXT_MAX_LENGTH } from '@shared/chat/chat-limits';
import { isCountShown, sendDisabledReason } from '@shared/chat/chat-composer/chat-composer.utils';

const IDLE = { isRunning: false, isStopping: false };

describe('chat-composer.utils', { tags: ['Chat'] }, () => {
  it('sendDisabledReason_WithAnEmptyText_ReturnsTheEmptyReason', () => {
    expect(sendDisabledReason({ text: '  ', ...IDLE })).toBe('CHAT.COMPOSER.REASON_EMPTY');
  });

  it('sendDisabledReason_WithATextOverTheLimit_ReturnsTheTooLongReason', () => {
    const text = 'a'.repeat(CHAT_TEXT_MAX_LENGTH + 1);

    expect(sendDisabledReason({ text, ...IDLE })).toBe('CHAT.COMPOSER.REASON_TOO_LONG');
  });

  it('sendDisabledReason_WithATextAtTheLimit_ReturnsNull', () => {
    const text = 'a'.repeat(CHAT_TEXT_MAX_LENGTH);

    expect(sendDisabledReason({ text, ...IDLE })).toBeNull();
  });

  it('sendDisabledReason_WhileRunning_ReturnsTheRunningReason', () => {
    expect(sendDisabledReason({ text: 'Hi', isRunning: true, isStopping: false })).toBe('CHAT.COMPOSER.REASON_RUNNING');
  });

  it('sendDisabledReason_WhileStopping_ReturnsTheStoppingReason', () => {
    expect(sendDisabledReason({ text: 'Hi', isRunning: true, isStopping: true })).toBe('CHAT.COMPOSER.REASON_STOPPING');
  });

  it('isCountShown_BelowAndFromTheShare_ChangesAtTheShare', () => {
    expect([isCountShown(1599), isCountShown(1600)]).toEqual([false, true]);
  });
});
