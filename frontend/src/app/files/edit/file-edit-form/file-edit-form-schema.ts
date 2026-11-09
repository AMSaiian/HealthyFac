import { type infer as Infer, maxLength, minLength, object, string, trim } from 'zod/mini';

import { DESCRIPTION_MAX_LENGTH, FILE_NAME_MAX_LENGTH } from '@files/files-limits';

export const REQUIRED_ERROR_KEY = 'FILES.EDIT_DIALOG.REQUIRED';
export const TOO_LONG_ERROR_KEY = 'FILES.EDIT_DIALOG.TOO_LONG';

/** The one schema of the edit form. The messages are translation keys. */
export const fileEditFormSchema = object({
  fileName: string().check(
    trim(),
    minLength(1, REQUIRED_ERROR_KEY),
    maxLength(FILE_NAME_MAX_LENGTH, TOO_LONG_ERROR_KEY),
  ),
  description: string().check(trim(), maxLength(DESCRIPTION_MAX_LENGTH, TOO_LONG_ERROR_KEY)),
});

export type FileEditFormValue = Infer<typeof fileEditFormSchema>;
