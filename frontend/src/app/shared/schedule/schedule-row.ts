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
  /** A locked bar cannot be dragged or resized on an editable board. */
  readonly isLocked?: boolean;
  /** The planned end of a work order in progress. The bar ends at now when now is later. */
  readonly plannedEnd?: Temporal.Instant;
  /** Whether the work order is in progress: its bar ends at the later of its end and now. */
  readonly isRunning?: boolean;
}

export interface ScheduleRow {
  readonly id: string;
  readonly label: string;
  readonly blocks: readonly ScheduleBlock[];
  readonly bars: readonly ScheduleBar[];
}
