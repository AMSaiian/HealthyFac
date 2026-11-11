import { dateRangeFilter, idSetFilter, setFilter, textFilter } from '@shared/grid/requests/grid-filters';

import type { SearchWorkOrdersBody, WorkOrderGridState } from '@work-orders/work-order';

export function toSearchBody(state: WorkOrderGridState, timeZoneId: string): SearchWorkOrdersBody {
  const { filter } = state;
  return {
    offset: state.offset,
    limit: state.limit,
    ...(state.sort === null ? {} : { sort: state.sort }),
    filter: {
      title: textFilter(filter.title),
      statuses: setFilter(filter.statuses),
      priorities: setFilter(filter.priorities),
      categories: setFilter(filter.categories),
      assignee: idSetFilter(filter.assigneeIds),
      machine: idSetFilter(filter.machineIds),
      plannedStart: dateRangeFilter(filter.plannedStart, timeZoneId),
    },
  };
}
