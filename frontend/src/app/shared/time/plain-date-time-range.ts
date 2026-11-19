import type { Temporal } from '@shared/time/temporal';

/** A range of local moments with minute precision. An end is `null` while the user has not picked it. */
export interface PlainDateTimeRange {
  readonly from: Temporal.PlainDateTime | null;
  readonly to: Temporal.PlainDateTime | null;
}

export const EMPTY_DATE_TIME_RANGE: PlainDateTimeRange = { from: null, to: null };
