import type { Temporal } from '@shared/time/temporal';

/** A work block of a person: the time that the person works. */
export interface ScheduleBlock {
  readonly start: Temporal.Instant;
  readonly end: Temporal.Instant;
}

/** A scheduled work order as a bar: from the planned start to the end of its occupancy. */
export interface ScheduleBar {
  readonly id: string;
  readonly title: string;
  readonly start: Temporal.Instant;
  readonly end: Temporal.Instant;
  /** The translation key of the status label. */
  readonly statusKey: string;
  /** The CSS class that colors the bar (one preset color per status). */
  readonly colorClass: string;
}

export interface ScheduleRow {
  readonly id: string;
  readonly label: string;
  readonly blocks: readonly ScheduleBlock[];
  readonly bars: readonly ScheduleBar[];
}
