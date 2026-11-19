import { Temporal } from '@shared/time/temporal';

import { type CaseSignal, SignalCoverage, SignalRule, SignalSeverity, SignalState } from '@maintenance/case';
import {
  chartQueryOf,
  groupKeyOf,
  RESOLVED_BEFORE_REVIEW,
  signalComparison,
  signalDurationSeconds,
  signalSubject,
} from '@maintenance/detail/case-signals/case-signals.utils';

const T10 = '2026-01-01T10:00:00Z';
const T12 = '2026-01-01T12:00:00Z';
const NOW = Temporal.Instant.from(T12).epochMilliseconds;

function signal(id: string, coverage: SignalCoverage, extra: Partial<CaseSignal> = {}): CaseSignal {
  return {
    id,
    rule: SignalRule.Threshold,
    severity: SignalSeverity.Alarm,
    state: SignalState.Open,
    coverage,
    metricKey: null,
    statusCode: null,
    value: null,
    limit: null,
    openedAt: '2026-01-01T00:00:00Z',
    resolvedAt: null,
    workOrderDraftId: null,
    workOrderId: null,
    ...extra,
  };
}

describe('case signals utils', () => {
  it.each([
    ['a resolved signal that no run reviewed', SignalCoverage.New, SignalState.Resolved, RESOLVED_BEFORE_REVIEW],
    ['an open new signal', SignalCoverage.New, SignalState.Open, SignalCoverage.New],
    ['a resolved dismissed signal', SignalCoverage.Dismissed, SignalState.Resolved, SignalCoverage.Dismissed],
  ])('groupKeyOf_Of%s_Is%s', (_name, coverage, state, expected) => {
    expect(groupKeyOf(signal('a', coverage, { state }))).toBe(expected);
  });

  it('signalSubject_MetricKeyAndStatusCode_PrefersTheMetricKey', () => {
    expect(signalSubject({ metricKey: 'spindle.temperature', statusCode: 'RUN' })).toBe('spindle.temperature');
    expect(signalSubject({ metricKey: null, statusCode: 'RUN' })).toBe('RUN');
    expect(signalSubject({ metricKey: null, statusCode: null })).toBe('');
  });

  it.each([
    [91, 90, '>'],
    [80, 90, '<'],
    [90, 90, '='],
    [null, 90, null],
    [91, null, null],
  ])('signalComparison_OfValue%sAndLimit%s_Is%s', (value, limit, sign) => {
    expect(signalComparison({ value, limit })).toBe(sign);
  });

  it('signalDurationSeconds_OfAnOpenSignal_IsNull', () => {
    expect(signalDurationSeconds({ openedAt: '2026-01-01T00:00:00Z', resolvedAt: null })).toBeNull();
  });

  it('signalDurationSeconds_OfAResolvedSignal_IsTheSpanInSeconds', () => {
    const seconds = signalDurationSeconds({ openedAt: '2026-01-01T00:00:00Z', resolvedAt: '2026-01-01T00:10:30Z' });

    expect(seconds).toBe(630);
  });

  it('chartQueryOf_OfAResolvedSignal_WidensItByTenPercentOnEachSide', () => {
    const query = chartQueryOf(signal('a', SignalCoverage.New, { openedAt: T10, resolvedAt: T12 }), NOW);

    expect(query).toEqual({ from: '2026-01-01T09:48:00Z', to: '2026-01-01T12:12:00Z' });
  });

  it('chartQueryOf_OfAnOpenSignal_RunsToNowWithTenPercentOnEachSide', () => {
    const query = chartQueryOf(signal('a', SignalCoverage.New, { openedAt: T10, resolvedAt: null }), NOW);

    expect(query).toEqual({ from: '2026-01-01T09:48:00Z', to: '2026-01-01T12:12:00Z' });
  });

  it.each([
    ['a short signal', '2026-01-01T10:10:00Z'],
    ['a zero-length signal', T10],
  ])('chartQueryOf_Of%s_IsOneHourCenteredOnTheSignal', (_name, resolvedAt) => {
    const query = chartQueryOf(signal('a', SignalCoverage.New, { openedAt: T10, resolvedAt }), NOW);

    const length =
      Temporal.Instant.from(query.to).epochMilliseconds - Temporal.Instant.from(query.from).epochMilliseconds;
    const center =
      (Temporal.Instant.from(query.from).epochMilliseconds + Temporal.Instant.from(query.to).epochMilliseconds) / 2;
    expect(length).toBe(3_600_000);
    expect(center).toBe(
      (Temporal.Instant.from(T10).epochMilliseconds + Temporal.Instant.from(resolvedAt).epochMilliseconds) / 2,
    );
  });

  it('chartQueryOf_OfASignalWithAMetric_CarriesTheMetricKey', () => {
    const query = chartQueryOf(
      signal('a', SignalCoverage.New, { openedAt: T10, resolvedAt: T12, metricKey: 'temp' }),
      NOW,
    );

    expect(query.metricKey).toBe('temp');
  });

  it('chartQueryOf_OfAStatusSignal_CarriesNoMetricKey', () => {
    const query = chartQueryOf(
      signal('a', SignalCoverage.New, { openedAt: T10, resolvedAt: T12, statusCode: 'S1' }),
      NOW,
    );

    expect('metricKey' in query).toBe(false);
  });
});
