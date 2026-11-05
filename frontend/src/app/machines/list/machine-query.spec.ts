import { SortDirection } from '@shared/grid/requests/grid-request';

import { type MachineFilter, type MachineGridState, MachineSortColumn, MachineState } from '@machines/machine';
import { toSearchBody } from './machine-query';

const ZONE = 'Europe/Kyiv';
const EMPTY_RANGE = { from: null, to: null };

const EMPTY_FILTER: MachineFilter = {
  name: '',
  serialNumber: '',
  departmentIds: [],
  states: [],
  createdAt: EMPTY_RANGE,
  updatedAt: EMPTY_RANGE,
};

function stateWith(filter: Partial<MachineFilter>): MachineGridState {
  return { offset: 0, limit: 25, sort: null, filter: { ...EMPTY_FILTER, ...filter } };
}

describe('toSearchBody', () => {
  it('toSearchBody_WithEmptyFilter_SendsNoActiveFilter', () => {
    const body = toSearchBody(stateWith({}), ZONE);

    expect(body.filter).toEqual({});
  });

  it('toSearchBody_WithFiltersSortAndPage_MapsEveryField', () => {
    const state: MachineGridState = {
      offset: 50,
      limit: 10,
      sort: { column: MachineSortColumn.Name, direction: SortDirection.Descending },
      filter: {
        ...EMPTY_FILTER,
        name: 'lat',
        serialNumber: 'ab',
        departmentIds: ['d1'],
        states: [MachineState.Operational],
      },
    };

    const body = toSearchBody(state, ZONE);

    expect(body).toEqual({
      offset: 50,
      limit: 10,
      sort: { column: 'name', direction: 'desc' },
      filter: {
        name: { value: 'lat' },
        serialNumber: { value: 'ab' },
        departmentIds: { ids: ['d1'] },
        states: { values: ['operational'] },
      },
    });
  });
});
