import { SortDirection } from '@shared/grid/requests/grid-request';
import { Temporal } from '@shared/time/temporal';

import {
  WorkOrderCategory,
  type WorkOrderGridState,
  WorkOrderPriority,
  WorkOrderSortColumn,
  WorkOrderStatus,
} from '@work-orders/work-order';

import { toSearchBody } from './work-order-query';
import { INITIAL_FILTER } from './work-orders-grid.store.utils';

const ZONE = 'Europe/Kyiv';

function stateWith(filter: Partial<WorkOrderGridState['filter']>): WorkOrderGridState {
  return {
    offset: 20,
    limit: 10,
    sort: { column: WorkOrderSortColumn.Title, direction: SortDirection.Ascending },
    filter: { ...INITIAL_FILTER, ...filter },
  };
}

describe('toSearchBody', () => {
  it('toSearchBody_WithEmptyFilter_LeavesEveryFilterOut', () => {
    const body = toSearchBody(stateWith({}), ZONE);

    expect(body.offset).toBe(20);
    expect(body.limit).toBe(10);
    expect(body.sort).toEqual({ column: 'title', direction: 'asc' });
    expect(JSON.parse(JSON.stringify(body.filter))).toEqual({});
  });

  it('toSearchBody_WithEveryFilter_BuildsEachPart', () => {
    const body = toSearchBody(
      stateWith({
        title: 'filter',
        statuses: [WorkOrderStatus.Staged],
        priorities: [WorkOrderPriority.High],
        categories: [WorkOrderCategory.Repair],
        assigneeIds: ['e1'],
        machineIds: ['m1', 'm2'],
      }),
      ZONE,
    );

    expect(body.filter).toEqual({
      title: { value: 'filter' },
      statuses: { values: ['staged'] },
      priorities: { values: ['high'] },
      categories: { values: ['repair'] },
      assignee: { ids: ['e1'] },
      machine: { ids: ['m1', 'm2'] },
    });
  });

  it('toSearchBody_WithAnyState_NeverSendsIsDelayed', () => {
    const body = toSearchBody(stateWith({ title: 'x' }), ZONE);

    expect(Object.keys(body.filter)).not.toContain('isDelayed');
  });

  it('toSearchBody_WithPlannedStartRange_UsesTheUtcRangeOfTheLocalDates', () => {
    const body = toSearchBody(
      stateWith({
        plannedStart: { from: Temporal.PlainDate.from('2027-03-28'), to: Temporal.PlainDate.from('2027-03-28') },
      }),
      ZONE,
    );

    expect(body.filter.plannedStart).toEqual({ from: '2027-03-27T22:00:00Z', to: '2027-03-28T21:00:00Z' });
  });

  it('toSearchBody_WithoutSort_LeavesTheSortOut', () => {
    const body = toSearchBody({ ...stateWith({}), sort: null }, ZONE);

    expect('sort' in body).toBe(false);
  });
});
