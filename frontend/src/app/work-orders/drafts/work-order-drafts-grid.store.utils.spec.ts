import { DraftState } from '@work-orders/drafts/work-order-draft';
import { INITIAL_GRID, toSearchBody } from '@work-orders/drafts/work-order-drafts-grid.store.utils';

describe('work order drafts grid store utils', () => {
  it('toSearchBody_InitialGrid_AsksForThePendingDraftsNewestFirst', () => {
    const body = toSearchBody({ offset: 0, limit: 25, sort: INITIAL_GRID.sort, filter: INITIAL_GRID.filter });

    expect(body).toEqual({
      offset: 0,
      limit: 25,
      sort: { column: 'proposedAt', direction: 'desc' },
      filter: { states: { values: [DraftState.Pending] } },
    });
  });

  it('toSearchBody_NoSortAndNoStates_OmitsThem', () => {
    const body = toSearchBody({ offset: 10, limit: 25, sort: null, filter: { states: [] } });

    expect(body).toEqual({ offset: 10, limit: 25, filter: {} });
  });
});
