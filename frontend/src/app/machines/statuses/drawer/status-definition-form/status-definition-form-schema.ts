import { type infer as Infer, maxLength, minLength, object, string, trim, enum as zodEnum } from 'zod/mini';

import { richTextSchema } from '@shared/rich-text/rich-text-schema';

import { StatusSeverity } from '@machines/statuses/status-definition';
import { CODE_MAX_LENGTH, DESCRIPTION_MAX_LENGTH } from '@machines/statuses/status-definition-limits';

export const REQUIRED_ERROR_KEY = 'FORMS.REQUIRED';
export const TOO_LONG_ERROR_KEY = 'FORMS.TOO_LONG';

/** The one schema of a status definition. The dialog form validates with it. The messages are translation keys. */
export const statusDefinitionFormSchema = object({
  code: string().check(trim(), minLength(1, REQUIRED_ERROR_KEY), maxLength(CODE_MAX_LENGTH, TOO_LONG_ERROR_KEY)),
  severity: zodEnum(StatusSeverity),
  description: richTextSchema(DESCRIPTION_MAX_LENGTH, TOO_LONG_ERROR_KEY),
});

export type StatusDefinitionFormValue = Infer<typeof statusDefinitionFormSchema>;
