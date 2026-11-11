export enum Weekday {
  Monday = 'monday',
  Tuesday = 'tuesday',
  Wednesday = 'wednesday',
  Thursday = 'thursday',
  Friday = 'friday',
  Saturday = 'saturday',
  Sunday = 'sunday',
}

/** A work block as `HH:mm` text. The API also returns `HH:mm:ss`; the store cuts the seconds. */
export interface ShiftBlock {
  readonly start: string;
  readonly end: string;
}

export interface ShiftDay {
  readonly weekday: Weekday;
  readonly blocks: readonly ShiftBlock[];
}

export interface ShiftVersion {
  readonly id: string;
  readonly effectiveFrom: string;
  readonly days: readonly ShiftDay[];
  readonly rowVersion: number;
}

export interface ShiftOverride {
  readonly id: string;
  readonly date: string;
  readonly blocks: readonly ShiftBlock[];
  readonly rowVersion: number;
}

export interface AffectedWorkOrder {
  readonly id: string;
  readonly title: string;
  readonly plannedStartLocal: string;
}

export interface CreateVersionBody {
  readonly effectiveFrom: string;
  readonly days: readonly ShiftDay[];
}

export interface UpdateVersionBody {
  readonly days: readonly ShiftDay[];
  readonly rowVersion: number;
}

export interface SetOverrideBody {
  readonly blocks: readonly ShiftBlock[];
  readonly rowVersion?: number;
}

export interface CreateVersionResponse {
  readonly id: string;
}
