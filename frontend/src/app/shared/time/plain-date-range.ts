import type { Temporal } from './temporal';

/** A range of calendar days. A `null` bound is open. */
export interface PlainDateRange {
  readonly from: Temporal.PlainDate | null;
  readonly to: Temporal.PlainDate | null;
}

export const EMPTY_RANGE: PlainDateRange = { from: null, to: null };

export function isRange(value: unknown): value is PlainDateRange {
  return typeof value === 'object' && value !== null && 'from' in value;
}

export function isRangeEmpty(range: PlainDateRange): boolean {
  return range.from === null && range.to === null;
}
