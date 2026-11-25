import { Temporal } from '@shared/time/temporal';

import { WORK_ORDER_DRAFTS_PATH, WORK_ORDERS_PATH } from '@work-orders/work-order-links';

import { type CaseSignal, SignalCoverage, SignalRule, SignalSeverity, SignalState } from '@maintenance/case';
import {
  chartQueryOf,
  coverageLink,
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

  it('chartQueryOf_OfASignalWhoseTenPercentIsNotAWholeMillisecond_RoundsTheRangeToMilliseconds', () => {
    const query = chartQueryOf(
      signal('a', SignalCoverage.New, { openedAt: T10, resolvedAt: '2026-01-01T19:09:07.003Z' }),
      NOW,
    );

    expect(query).toEqual({ from: '2026-01-01T09:05:05.3Z', to: '2026-01-01T20:04:01.703Z' });
  });

  it('chartQueryOf_OfAnOpenSignalAndAnOddNow_RoundsTheRangeToMilliseconds', () => {
    const query = chartQueryOf(signal('a', SignalCoverage.New, { openedAt: T10, resolvedAt: null }), NOW + 7_003);

    expect(query).toEqual({ from: '2026-01-01T09:47:59.3Z', to: '2026-01-01T12:12:07.703Z' });
  });

  it('chartQueryOf_OfAShortSignalWithAnOddCenter_RoundsTheHourToMilliseconds', () => {
    const query = chartQueryOf(
      signal('a', SignalCoverage.New, { openedAt: '2026-01-01T10:00:00.001Z', resolvedAt: '2026-01-01T10:00:00.004Z' }),
      NOW,
    );

    expect(query).toEqual({ from: '2026-01-01T09:30:00.003Z', to: '2026-01-01T10:30:00.003Z' });
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

  it('coverageLink_OfADraftedSignalWithADraft_OpensTheDraftsPageOnThatDraft', () => {
    const link = coverageLink(signal('a', SignalCoverage.Drafted, { workOrderDraftId: 'd1' }));

    expect(link).toEqual({
      path: WORK_ORDER_DRAFTS_PATH,
      queryParams: { draftId: 'd1' },
      tooltipKey: 'MAINTENANCE.OUTCOME.TOOLTIPS.OPEN_DRAFT',
      icon: 'pi pi-file-edit',
    });
  });

  it('coverageLink_OfALinkedSignalWithAWorkOrder_OpensTheWorkOrders', () => {
    const link = coverageLink(signal('a', SignalCoverage.Linked, { workOrderId: 'w1' }));

    expect(link).toEqual({
      path: WORK_ORDERS_PATH,
      queryParams: null,
      tooltipKey: 'MAINTENANCE.OUTCOME.TOOLTIPS.OPEN_WORK_ORDER',
      icon: 'pi pi-wrench',
    });
  });

  it.each([
    ['a new signal', signal('a', SignalCoverage.New)],
    ['a watched signal', signal('a', SignalCoverage.Watched)],
    ['a dismissed signal', signal('a', SignalCoverage.Dismissed)],
    ['a drafted signal without a draft', signal('a', SignalCoverage.Drafted)],
    ['a linked signal without a work order', signal('a', SignalCoverage.Linked)],
  ])('coverageLink_Of%s_IsNull', (_name, item) => {
    expect(coverageLink(item)).toBeNull();
  });
});
