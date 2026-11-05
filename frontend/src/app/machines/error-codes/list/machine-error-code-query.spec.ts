import { SortDirection } from '@shared/grid/requests/grid-request';
import { Temporal } from '@shared/time/temporal';

import {
  type MachineErrorCodeFilter,
  type MachineErrorCodeGridState,
  MachineErrorCodeSortColumn,
} from '@machines/error-codes/machine-error-code';
import { toSearchBody } from './machine-error-code-query';

const ZONE = 'Europe/Kyiv';
const EMPTY_RANGE = { from: null, to: null };
const DAY = Temporal.PlainDate.from('2026-03-29');

const EMPTY_FILTER: MachineErrorCodeFilter = { code: '', createdAt: EMPTY_RANGE, updatedAt: EMPTY_RANGE };

describe('toSearchBody', () => {
  it('toSearchBody_WithEmptyFilter_SendsNoActiveFilter', () => {
    const state: MachineErrorCodeGridState = { offset: 0, limit: 25, sort: null, filter: EMPTY_FILTER };

    const body = toSearchBody(state, ZONE);

    expect(body.filter).toEqual({});
  });

  it('toSearchBody_WithCodeSortAndPage_MapsEveryField', () => {
    const state: MachineErrorCodeGridState = {
      offset: 50,
      limit: 10,
      sort: { column: MachineErrorCodeSortColumn.Code, direction: SortDirection.Descending },
      filter: { ...EMPTY_FILTER, code: 'e-1' },
    };

    const body = toSearchBody(state, ZONE);

    expect(body).toEqual({
      offset: 50,
      limit: 10,
      sort: { column: 'code', direction: 'desc' },
      filter: { code: { value: 'e-1' } },
    });
  });

  it('toSearchBody_WithCreatedRange_SendsHalfOpenUtcRange', () => {
    const state: MachineErrorCodeGridState = {
      offset: 0,
      limit: 25,
      sort: null,
      filter: { ...EMPTY_FILTER, createdAt: { from: DAY, to: DAY } },
    };

    const body = toSearchBody(state, ZONE);

    expect(body.filter.createdAt).toEqual({ from: '2026-03-28T22:00:00Z', to: '2026-03-29T21:00:00Z' });
  });
});
