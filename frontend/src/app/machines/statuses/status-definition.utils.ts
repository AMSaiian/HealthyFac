import { StatusSeverity } from '@machines/statuses/status-definition';

export const SEVERITY_LABEL_KEYS: Readonly<Record<StatusSeverity, string>> = {
  [StatusSeverity.Info]: 'STATUS_DEFINITIONS.SEVERITIES.INFO',
  [StatusSeverity.Warning]: 'STATUS_DEFINITIONS.SEVERITIES.WARNING',
  [StatusSeverity.Error]: 'STATUS_DEFINITIONS.SEVERITIES.ERROR',
};

export const SEVERITY_OPTIONS: { readonly value: StatusSeverity; readonly labelKey: string }[] = Object.values(
  StatusSeverity,
).map((value) => ({ value, labelKey: SEVERITY_LABEL_KEYS[value] }));
