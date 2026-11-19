import { Temporal } from '@shared/time/temporal';

import { type CaseSignal, SignalCoverage, SignalState } from '@maintenance/case';

const MILLISECONDS_PER_SECOND = 1000;

function epochMs(instant: string): number {
  return Temporal.Instant.from(instant).epochMilliseconds;
}

/** The key of the group of the signals that resolved before any review (coverage `new`, state `resolved`). */
export const RESOLVED_BEFORE_REVIEW = 'resolvedBeforeReview';

export type CoverageGroupKey = SignalCoverage | typeof RESOLVED_BEFORE_REVIEW;

export const COVERAGE_LABEL_KEYS: Readonly<Record<CoverageGroupKey, string>> = {
  [SignalCoverage.New]: 'MAINTENANCE.COVERAGE.NEW',
  [SignalCoverage.Drafted]: 'MAINTENANCE.COVERAGE.DRAFTED',
  [SignalCoverage.Linked]: 'MAINTENANCE.COVERAGE.LINKED',
  [SignalCoverage.Watched]: 'MAINTENANCE.COVERAGE.WATCHED',
  [SignalCoverage.Dismissed]: 'MAINTENANCE.COVERAGE.DISMISSED',
  [RESOLVED_BEFORE_REVIEW]: 'MAINTENANCE.COVERAGE.RESOLVED_BEFORE_REVIEW',
};

/** The group of a signal. A resolved signal that no run reviewed (coverage `new`) is not "not yet reviewed". */
export function groupKeyOf(signal: Pick<CaseSignal, 'coverage' | 'state'>): CoverageGroupKey {
  return signal.coverage === SignalCoverage.New && signal.state === SignalState.Resolved
    ? RESOLVED_BEFORE_REVIEW
    : signal.coverage;
}

/** The text that names the subject of a signal: its metric key, else its status code, else an empty text. */
export function signalSubject(signal: Pick<CaseSignal, 'metricKey' | 'statusCode'>): string {
  return signal.metricKey ?? signal.statusCode ?? '';
}

/** The sign that sets a signal value against its limit: `>` above, `<` below, `=` equal; `null` without both. */
export function signalComparison(signal: Pick<CaseSignal, 'value' | 'limit'>): '>' | '<' | '=' | null {
  if (signal.value === null || signal.limit === null) {
    return null;
  }
  if (signal.value > signal.limit) {
    return '>';
  }
  return signal.value < signal.limit ? '<' : '=';
}

/** The seconds from the start to the end of a signal; `null` while it is open. */
export function signalDurationSeconds(signal: Pick<CaseSignal, 'openedAt' | 'resolvedAt'>): number | null {
  if (signal.resolvedAt === null) {
    return null;
  }
  const seconds = (epochMs(signal.resolvedAt) - epochMs(signal.openedAt)) / MILLISECONDS_PER_SECOND;
  return Math.max(0, seconds);
}

/** The query of the dashboards link of a signal. */
export interface ChartQuery {
  readonly from: string;
  readonly to: string;
  readonly metricKey?: string;
}

/** The widening of the range on each side: 10 % of the length of the signal (Artur 2026-11-19). */
const RANGE_MARGIN_SHARE = 0.1;
/** The shortest range that the link opens: the smallest preset of the chart (`ChartRange.Hour`). */
const MIN_RANGE_MS = 3_600_000;

/**
 * The dashboards link of a signal: from its start to its end (to `nowMs` while it is open), widened by 10 % on each
 * side. A range below one hour becomes one hour centered on the signal. The link carries the metric key, if any.
 */
export function chartQueryOf(
  signal: Pick<CaseSignal, 'openedAt' | 'resolvedAt' | 'metricKey'>,
  nowMs: number,
): ChartQuery {
  const start = epochMs(signal.openedAt);
  const end = signal.resolvedAt === null ? Math.max(nowMs, start) : Math.max(epochMs(signal.resolvedAt), start);
  const margin = (end - start) * RANGE_MARGIN_SHARE;
  const isShort = end - start + 2 * margin < MIN_RANGE_MS;
  const center = (start + end) / 2;
  const from = isShort ? center - MIN_RANGE_MS / 2 : start - margin;
  const to = isShort ? center + MIN_RANGE_MS / 2 : end + margin;
  const range = {
    from: Temporal.Instant.fromEpochMilliseconds(from).toString(),
    to: Temporal.Instant.fromEpochMilliseconds(to).toString(),
  };
  return signal.metricKey === null ? range : { ...range, metricKey: signal.metricKey };
}
