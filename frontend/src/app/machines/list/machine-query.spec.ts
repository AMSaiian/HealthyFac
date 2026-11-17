import { SortDirection } from '@shared/grid/requests/grid-request';

import { Temporal } from '@shared/time/temporal';

import { type MachineFilter, type MachineGridState, MachineSortColumn, MachineState } from '@machines/machine';
import { toSearchBody } from './machine-query';

const ZONE = 'Europe/Kyiv';
const EMPTY_RANGE = { from: null, to: null };

const EMPTY_FILTER: MachineFilter = {
  name: '',
  serialNumber: '',
  gatewayId: '',
  states: [],
  expectedGoLiveOn: EMPTY_RANGE,
  createdAt: EMPTY_RANGE,
  updatedAt: EMPTY_RANGE,
  hasPendingDrafts: null,
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
        states: { values: ['operational'] },
      },
    });
  });

  it('toSearchBody_WithGoLiveRange_SendsHalfOpenDaysWithoutZone', () => {
    const range = { from: Temporal.PlainDate.from('2026-03-01'), to: Temporal.PlainDate.from('2026-03-10') };

    const body = toSearchBody(stateWith({ expectedGoLiveOn: range }), ZONE);

    expect(body.filter.expectedGoLiveOn).toEqual({ from: '2026-03-01', to: '2026-03-11' });
  });

  it('toSearchBody_WithGoLiveSort_SendsTheSortColumn', () => {
    const state: MachineGridState = {
      ...stateWith({}),
      sort: { column: MachineSortColumn.ExpectedGoLiveOn, direction: SortDirection.Ascending },
    };

    const body = toSearchBody(state, ZONE);

    expect(body.sort).toEqual({ column: 'expectedGoLiveOn', direction: 'asc' });
  });

  it('toSearchBody_WithGatewayIdFilter_AddsStartsWithFilter', () => {
    const body = toSearchBody(stateWith({ gatewayId: 'lmno' }), ZONE);

    expect(body.filter.gatewayId).toEqual({ value: 'lmno' });
  });

  it.each([true, false])('toSearchBody_WithHasPendingDrafts%s_SendsTheFlag', (hasPendingDrafts) => {
    const body = toSearchBody(stateWith({ hasPendingDrafts }), ZONE);

    expect(body.filter.hasPendingDrafts).toBe(hasPendingDrafts);
  });

  it('toSearchBody_WithoutHasPendingDrafts_SendsNoFlag', () => {
    const body = toSearchBody(stateWith({}), ZONE);

    expect(body.filter.hasPendingDrafts).toBeUndefined();
  });
});
