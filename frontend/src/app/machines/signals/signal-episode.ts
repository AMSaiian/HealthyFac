/** The severities of a signal episode. The backend emits the same literals. */
export enum SignalSeverity {
  Warning = 'warning',
  Alarm = 'alarm',
}

export enum SignalEpisodeState {
  Open = 'open',
  Resolved = 'resolved',
}

export interface SignalEpisode {
  readonly episodeId: string;
  readonly rule: string;
  readonly severity: SignalSeverity;
  readonly state: SignalEpisodeState;
  readonly metricDefinitionId: string | null;
  readonly metricKey: string | null;
  readonly statusDefinitionId: string | null;
  readonly statusCode: string | null;
  readonly phaseCode: string | null;
  readonly openedAt: string;
  readonly resolvedAt: string | null;
}

export interface SignalEpisodesResponse {
  readonly items: readonly SignalEpisode[];
}

/** One bucket of the metric history: the values of the readings from `start` to the start of the next bucket. */
export interface MetricBucket {
  readonly start: string;
  readonly avg: number;
  readonly min: number;
  readonly max: number;
  readonly count: number;
}

export interface MetricHistoryResponse {
  readonly buckets: readonly MetricBucket[];
}

/** The time of a machine in one phase over a range. A deleted phase keeps its name. */
export interface PhaseTime {
  readonly phaseId: string;
  readonly code: string;
  readonly name: string;
  readonly isTracked: boolean;
  readonly isDeleted: boolean;
  readonly seconds: number;
}

/** The time in each phase over a range. With the unknown mode and the no-data seconds, it adds up to the range. */
export interface PhaseTimesResponse {
  readonly phases: readonly PhaseTime[];
  readonly unknownModeSeconds: number;
  readonly noDataSeconds: number;
}

/** The time of a machine in one state over a range. The state is the value of `MachineState`. */
export interface MachineStateTime {
  readonly state: string;
  readonly seconds: number;
}

/** The time in each machine state over a range, plus the time before the machine existed. */
export interface MachineStateTimesResponse {
  readonly states: readonly MachineStateTime[];
  readonly beforeCreationSeconds: number;
}

/** The ranges that the chart offers. The value is the length in hours. */
export enum ChartRange {
  Hour = 'hour',
  SixHours = 'sixHours',
  Day = 'day',
}
