import { dateRangeFilter, setFilter, textFilter } from '@shared/grid/requests/grid-filters';

import type { SearchStatusDefinitionsBody, StatusDefinitionGridState } from '@machines/statuses/status-definition';

export function toSearchBody(state: StatusDefinitionGridState, timeZoneId: string): SearchStatusDefinitionsBody {
  const { filter } = state;
  return {
    offset: state.offset,
    limit: state.limit,
    ...(state.sort === null ? {} : { sort: state.sort }),
    filter: {
      code: textFilter(filter.code),
      severities: setFilter(filter.severities),
      createdAt: dateRangeFilter(filter.createdAt, timeZoneId),
      updatedAt: dateRangeFilter(filter.updatedAt, timeZoneId),
    },
  };
}
