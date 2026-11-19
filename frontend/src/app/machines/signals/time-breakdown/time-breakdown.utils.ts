import type { MachineStateTimesResponse, PhaseTimesResponse } from '@machines/signals/signal-episode';

const PERCENT_TOTAL = 100;
/** Rounds the remainders of the shares, so a floating point difference never decides a tie. */
const REMAINDER_PRECISION = 1_000_000;
const SECONDS_PER_MINUTE = 60;
const MINUTES_PER_HOUR = 60;

/** What a row of the breakdown is: a phase or a state, or one of the rest rows. */
export enum TimeRowKind {
  Item = 'item',
  UnknownMode = 'unknownMode',
  NoData = 'noData',
  BeforeCreation = 'beforeCreation',
}

/** One row of the stacked bar and of the table. */
export interface TimeRow {
  readonly key: string;
  readonly label: string;
  readonly kind: TimeRowKind;
  readonly seconds: number;
  /** The share of the range in whole percent. The shares of all rows add up to 100. */
  readonly share: number;
  /** True for a phase that does not teach the statistics. */
  readonly isUntracked: boolean;
}

/** The texts that the rows need. The caller translates them. */
export interface PhaseRowLabels {
  readonly unknownMode: string;
  readonly noData: string;
  readonly deleted: string;
}

export interface StateRowLabels {
  readonly stateOf: (value: string) => string;
  readonly beforeCreation: string;
}

/**
 * Gives each value its share of the total in whole percent, so the shares add up to 100 (the largest remainder
 * method). All zeros give all zeros.
 */
export function sharesOf(values: readonly number[]): number[] {
  const total = values.reduce((sum, value) => sum + value, 0);
  if (total <= 0) {
    return values.map(() => 0);
  }
  const exact = values.map((value) => (value / total) * PERCENT_TOTAL);
  const shares = exact.map((value) => Math.floor(value));
  const missing = PERCENT_TOTAL - shares.reduce((sum, share) => sum + share, 0);
  const byRemainder = exact
    .map((value, index) => ({ index, remainder: Math.round((value - Math.floor(value)) * REMAINDER_PRECISION) }))
    .sort((first, second) => second.remainder - first.remainder || first.index - second.index);
  for (const { index } of byRemainder.slice(0, missing)) {
    shares[index] += 1;
  }
  return shares;
}

/** The length of the bar axis: the sum of all rows, so the rows that hold 100 % fill the whole width. */
export function barTotal(rows: readonly Pick<TimeRow, 'seconds'>[]): number {
  return rows.reduce((sum, row) => sum + row.seconds, 0);
}

/** Splits seconds into whole hours and the minutes that remain. */
export function durationParts(seconds: number): { hours: number; minutes: number } {
  const totalMinutes = Math.round(seconds / SECONDS_PER_MINUTE);
  return { hours: Math.floor(totalMinutes / MINUTES_PER_HOUR), minutes: totalMinutes % MINUTES_PER_HOUR };
}

/** The theme tokens of the item segments. A phase or a state takes the next one, in order. */
export const SEGMENT_COLOR_TOKENS: readonly string[] = [
  '--p-primary-color',
  '--p-orange-500',
  '--p-green-500',
  '--p-blue-500',
  '--p-purple-500',
  '--p-teal-500',
  '--p-pink-500',
  '--p-yellow-500',
];

const UNKNOWN_MODE_COLOR_TOKEN = '--p-red-300';
const NO_DATA_COLOR_TOKEN = '--p-surface-400';
const BEFORE_CREATION_COLOR_TOKEN = '--p-surface-300';

/** The theme token of the color of each row, in the order of the rows. The rest rows have neutral colors. */
export function colorTokensOf(rows: readonly Pick<TimeRow, 'kind'>[]): string[] {
  let itemIndex = 0;
  return rows.map((row) => {
    switch (row.kind) {
      case TimeRowKind.UnknownMode:
        return UNKNOWN_MODE_COLOR_TOKEN;
      case TimeRowKind.NoData:
        return NO_DATA_COLOR_TOKEN;
      case TimeRowKind.BeforeCreation:
        return BEFORE_CREATION_COLOR_TOKEN;
      case TimeRowKind.Item:
        return SEGMENT_COLOR_TOKENS[itemIndex++ % SEGMENT_COLOR_TOKENS.length];
    }
  });
}

function withShares(rows: readonly Omit<TimeRow, 'share'>[]): TimeRow[] {
  const shares = sharesOf(rows.map((row) => row.seconds));
  return rows.map((row, index) => ({ ...row, share: shares[index] }));
}

/** The rows of the phases: each phase, then the unknown mode, then the time without data. */
export function toPhaseRows(response: PhaseTimesResponse, labels: PhaseRowLabels): TimeRow[] {
  return withShares([
    ...response.phases.map((phase) => ({
      key: phase.phaseId,
      label: phase.isDeleted ? `${phase.name} (${labels.deleted})` : phase.name,
      kind: TimeRowKind.Item,
      seconds: phase.seconds,
      isUntracked: !phase.isTracked,
    })),
    {
      key: TimeRowKind.UnknownMode,
      label: labels.unknownMode,
      kind: TimeRowKind.UnknownMode,
      seconds: response.unknownModeSeconds,
      isUntracked: false,
    },
    {
      key: TimeRowKind.NoData,
      label: labels.noData,
      kind: TimeRowKind.NoData,
      seconds: response.noDataSeconds,
      isUntracked: false,
    },
  ]);
}

/** The rows of the states: each state, then the time before the machine existed. */
export function toStateRows(response: MachineStateTimesResponse, labels: StateRowLabels): TimeRow[] {
  return withShares([
    ...response.states.map((state) => ({
      key: state.state,
      label: labels.stateOf(state.state),
      kind: TimeRowKind.Item,
      seconds: state.seconds,
      isUntracked: false,
    })),
    {
      key: TimeRowKind.BeforeCreation,
      label: labels.beforeCreation,
      kind: TimeRowKind.BeforeCreation,
      seconds: response.beforeCreationSeconds,
      isUntracked: false,
    },
  ]);
}
