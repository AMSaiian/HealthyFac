import type { WorkOrderHistoryRow } from '@work-orders/work-order';

/** A history row with the estimate change that it made: `null` when the older row has the same estimate. */
export interface HistoryEntry {
  readonly row: WorkOrderHistoryRow;
  readonly estimateChange: { readonly oldMinutes: number; readonly newMinutes: number } | null;
}

/**
 * Lists the rows newest first. A row whose estimate differs from the row of the previous revision carries the old and
 * the new estimate. The oldest row has no previous row, so it carries none.
 */
export function toHistoryEntries(rows: readonly WorkOrderHistoryRow[]): HistoryEntry[] {
  const oldestFirst = [...rows].sort((a, b) => a.revision - b.revision);
  return oldestFirst
    .map((row, index): HistoryEntry => {
      const previous = index === 0 ? undefined : oldestFirst[index - 1];
      return {
        row,
        estimateChange:
          previous === undefined || previous.estimateMinutes === row.estimateMinutes
            ? null
            : { oldMinutes: previous.estimateMinutes, newMinutes: row.estimateMinutes },
      };
    })
    .reverse();
}
