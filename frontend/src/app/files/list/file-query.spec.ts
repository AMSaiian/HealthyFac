import { SortDirection } from '@shared/grid/requests/grid-request';
import { Temporal } from '@shared/time/temporal';

import { type FileFilter, type FileGridState, FileSortColumn } from '@files/file';

import { toSearchBody } from './file-query';

const ZONE = 'Europe/Kyiv';
const EMPTY_RANGE = { from: null, to: null };

const EMPTY_FILTER: FileFilter = { fileName: '', extensions: [], uploadedAt: EMPTY_RANGE };

describe('toSearchBody', () => {
  it('toSearchBody_WithEmptyFilter_SendsNoActiveFilter', () => {
    const state: FileGridState = { offset: 0, limit: 25, sort: null, filter: EMPTY_FILTER };

    const body = toSearchBody(state, ZONE);

    expect(body.filter).toEqual({});
    expect('sort' in body).toBe(false);
  });

  it('toSearchBody_WithNameExtensionsSortAndPage_MapsEveryField', () => {
    const state: FileGridState = {
      offset: 50,
      limit: 10,
      sort: { column: FileSortColumn.SizeBytes, direction: SortDirection.Descending },
      filter: { ...EMPTY_FILTER, fileName: 'man', extensions: ['pdf', 'png'] },
    };

    const body = toSearchBody(state, ZONE);

    expect(body).toEqual({
      offset: 50,
      limit: 10,
      sort: { column: FileSortColumn.SizeBytes, direction: SortDirection.Descending },
      filter: { fileName: { value: 'man' }, extension: { values: ['pdf', 'png'] } },
    });
  });

  it('toSearchBody_WithUploadRange_SendsTheHalfOpenUtcRangeOfTheLocalDays', () => {
    const state: FileGridState = {
      offset: 0,
      limit: 25,
      sort: null,
      filter: {
        ...EMPTY_FILTER,
        uploadedAt: { from: Temporal.PlainDate.from('2026-03-29'), to: Temporal.PlainDate.from('2026-03-30') },
      },
    };

    const body = toSearchBody(state, ZONE);

    expect(body.filter.uploadedAt).toEqual({ from: '2026-03-28T22:00:00Z', to: '2026-03-30T21:00:00Z' });
  });
});
