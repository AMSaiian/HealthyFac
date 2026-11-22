import { type WorkOrderHistoryRow, WorkOrderStatus } from '@work-orders/work-order';

import { toHistoryEntries } from './work-order-history.utils';

function row(revision: number, estimateMinutes: number): WorkOrderHistoryRow {
  return {
    revision,
    status: WorkOrderStatus.Backlog,
    assigneeId: null,
    assigneeName: null,
    plannedStartLocal: null,
    plannedStartUtc: null,
    estimateMinutes,
    createdAt: '2027-06-01T08:00:00Z',
    createdBy: 'u1',
  };
}

describe('toHistoryEntries', () => {
  it('toHistoryEntries_WithAChangedEstimate_CarriesTheOldAndTheNewOneOnTheNewerRow', () => {
    const entries = toHistoryEntries([row(1, 60), row(2, 90)]);

    expect(entries[0].row.revision).toBe(2);
    expect(entries[0].estimateChange).toEqual({ oldMinutes: 60, newMinutes: 90 });
    expect(entries[1].estimateChange).toBeNull();
  });

  it('toHistoryEntries_OnAPageWithOneRow_CarriesNoChangeForTheRowAtThePageBorder', () => {
    const entries = toHistoryEntries([row(14, 90)]);

    expect(entries.map((entry) => entry.estimateChange)).toEqual([null]);
  });

  it('toHistoryEntries_WithEqualEstimates_CarriesNoChange', () => {
    const entries = toHistoryEntries([row(2, 60), row(1, 60)]);

    expect(entries.map((entry) => entry.estimateChange)).toEqual([null, null]);
  });
});
