import { CHAT_TEXT_MAX_LENGTH } from '@shared/chat/chat-limits';

/** The share of the limit from which the character count shows. */
const COUNT_SHOWN_FROM = 0.8;

export interface ComposerState {
  readonly text: string;
  readonly isRunning: boolean;
  readonly isStopping: boolean;
}

/** The translation key of the reason why Send is disabled, or `null` when the message can be sent. */
export function sendDisabledReason(state: ComposerState): string | null {
  if (state.isStopping) {
    return 'CHAT.COMPOSER.REASON_STOPPING';
  }
  if (state.isRunning) {
    return 'CHAT.COMPOSER.REASON_RUNNING';
  }
  const length = state.text.trim().length;
  if (length === 0) {
    return 'CHAT.COMPOSER.REASON_EMPTY';
  }
  return length > CHAT_TEXT_MAX_LENGTH ? 'CHAT.COMPOSER.REASON_TOO_LONG' : null;
}

/** Tells whether the character count shows: near the limit and beyond it. */
export function isCountShown(textLength: number): boolean {
  return textLength >= CHAT_TEXT_MAX_LENGTH * COUNT_SHOWN_FROM;
}
