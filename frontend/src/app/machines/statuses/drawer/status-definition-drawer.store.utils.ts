import { type StatusDefinition, StatusSeverity } from '@machines/statuses/status-definition';

import type { StatusDefinitionFormValue } from '@machines/statuses/drawer/status-definition-form/status-definition-form-schema';

export const EMPTY_VALUE: StatusDefinitionFormValue = { code: '', severity: StatusSeverity.Info, description: null };

export function toFormValue(statusDefinition: StatusDefinition): StatusDefinitionFormValue {
  return {
    code: statusDefinition.code,
    severity: statusDefinition.severity,
    description: statusDefinition.description,
  };
}
