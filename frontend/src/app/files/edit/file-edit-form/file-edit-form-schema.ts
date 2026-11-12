import { type infer as Infer, maxLength, minLength, object, string, trim } from 'zod/mini';

import { richTextSchema } from '@shared/rich-text/rich-text-schema';

import { DESCRIPTION_MAX_LENGTH, FILE_NAME_MAX_LENGTH } from '@files/files-limits';

export const REQUIRED_ERROR_KEY = 'FORMS.REQUIRED';
export const TOO_LONG_ERROR_KEY = 'FORMS.TOO_LONG';

/** The one schema of the edit form. The messages are translation keys. */
export const fileEditFormSchema = object({
  fileName: string().check(
    trim(),
    minLength(1, REQUIRED_ERROR_KEY),
    maxLength(FILE_NAME_MAX_LENGTH, TOO_LONG_ERROR_KEY),
  ),
  description: richTextSchema(DESCRIPTION_MAX_LENGTH, TOO_LONG_ERROR_KEY),
});

export type FileEditFormValue = Infer<typeof fileEditFormSchema>;
