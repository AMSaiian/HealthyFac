import { asList, asRange, asText } from '@shared/grid/table/table-filters';
import { isRangeEmpty } from '@shared/time/plain-date-range';

import type { StatusSeverity } from '@machines/statuses/status-definition';
import {
  StatusDefinitionColumn,
  type StatusDefinitionFilter,
  StatusDefinitionSortColumn,
} from '@machines/statuses/status-definition';
import { SEVERITY_LABEL_KEYS } from '@machines/statuses/status-definition.utils';

export { SEVERITY_LABEL_KEYS };

export const COLUMN_LABEL_KEYS: Readonly<Record<StatusDefinitionColumn, string>> = {
  [StatusDefinitionColumn.Code]: 'STATUS_DEFINITIONS.COLUMNS.CODE',
  [StatusDefinitionColumn.Severity]: 'STATUS_DEFINITIONS.COLUMNS.SEVERITY',
  [StatusDefinitionColumn.CreatedAt]: 'STATUS_DEFINITIONS.COLUMNS.CREATED_AT',
  [StatusDefinitionColumn.UpdatedAt]: 'STATUS_DEFINITIONS.COLUMNS.UPDATED_AT',
};

export const MENU_UPDATE_KEY = 'STATUS_DEFINITIONS.UPDATE';
export const MENU_DELETE_KEY = 'STATUS_DEFINITIONS.DELETE';

export const SORTABLE_COLUMNS: readonly string[] = Object.values(StatusDefinitionSortColumn);

/** Maps the pending value of one column filter of the table to the store filter. A missing value means "empty". */
export const FILTER_READERS: Readonly<
  Record<StatusDefinitionColumn, (value: unknown) => Partial<StatusDefinitionFilter>>
> = {
  [StatusDefinitionColumn.Code]: (value) => ({ code: asText(value) }),
  [StatusDefinitionColumn.Severity]: (value) => ({ severities: asList<StatusSeverity>(value) }),
  [StatusDefinitionColumn.CreatedAt]: (value) => ({ createdAt: asRange(value) }),
  [StatusDefinitionColumn.UpdatedAt]: (value) => ({ updatedAt: asRange(value) }),
};

/** Reads the applied store value of one column as a table filter value. `null` means "no filter". */
export const FILTER_WRITERS: Readonly<Record<StatusDefinitionColumn, (filter: StatusDefinitionFilter) => unknown>> = {
  [StatusDefinitionColumn.Code]: (filter) => filter.code || null,
  [StatusDefinitionColumn.Severity]: (filter) => (filter.severities.length === 0 ? null : filter.severities),
  [StatusDefinitionColumn.CreatedAt]: (filter) => (isRangeEmpty(filter.createdAt) ? null : filter.createdAt),
  [StatusDefinitionColumn.UpdatedAt]: (filter) => (isRangeEmpty(filter.updatedAt) ? null : filter.updatedAt),
};
