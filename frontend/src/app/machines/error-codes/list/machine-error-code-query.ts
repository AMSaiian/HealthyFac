import { dateRangeFilter, textFilter } from '@shared/grid/requests/grid-filters';

import type { MachineErrorCodeGridState, SearchMachineErrorCodesBody } from '@machines/error-codes/machine-error-code';

export function toSearchBody(state: MachineErrorCodeGridState, timeZoneId: string): SearchMachineErrorCodesBody {
  const { filter } = state;
  return {
    offset: state.offset,
    limit: state.limit,
    ...(state.sort === null ? {} : { sort: state.sort }),
    filter: {
      code: textFilter(filter.code),
      createdAt: dateRangeFilter(filter.createdAt, timeZoneId),
      updatedAt: dateRangeFilter(filter.updatedAt, timeZoneId),
    },
  };
}
