import { maxLength, minLength, object, string, trim } from 'zod/mini';

import { CHAT_TEXT_MAX_LENGTH } from '@shared/chat/chat-limits';

const REQUIRED_ERROR_KEY = 'FORMS.REQUIRED';
const TOO_LONG_ERROR_KEY = 'FORMS.TOO_LONG';

export const chatComposerSchema = object({
  text: string().check(trim(), minLength(1, REQUIRED_ERROR_KEY), maxLength(CHAT_TEXT_MAX_LENGTH, TOO_LONG_ERROR_KEY)),
});
