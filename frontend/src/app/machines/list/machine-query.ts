import { dateRangeFilter, idSetFilter, setFilter, textFilter } from '@shared/grid/requests/grid-filters';

import type { MachineGridState, SearchMachinesBody } from '@machines/machine';

export function toSearchBody(state: MachineGridState, timeZoneId: string): SearchMachinesBody {
  const { filter } = state;
  return {
    offset: state.offset,
    limit: state.limit,
    ...(state.sort === null ? {} : { sort: state.sort }),
    filter: {
      name: textFilter(filter.name),
      serialNumber: textFilter(filter.serialNumber),
      departmentIds: idSetFilter(filter.departmentIds),
      states: setFilter(filter.states),
      createdAt: dateRangeFilter(filter.createdAt, timeZoneId),
      updatedAt: dateRangeFilter(filter.updatedAt, timeZoneId),
    },
  };
}
