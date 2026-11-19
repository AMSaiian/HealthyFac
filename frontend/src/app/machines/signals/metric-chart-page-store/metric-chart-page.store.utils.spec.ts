import { type MetricDefinition, MetricValueKind } from '@machines/metrics/metric-definition';
import {
  bucketCountForWidth,
  chartableMetrics,
  customBounds,
  MAX_BUCKET_COUNT,
  rangeBounds,
  selectionBounds,
  toPlainRange,
  zoomedBounds,
} from '@machines/signals/metric-chart-page-store/metric-chart-page.store.utils';
import { ChartRange } from '@machines/signals/signal-episode';
import { Temporal } from '@shared/time/temporal';

const KYIV = 'Europe/Kyiv';

describe('metric chart page store utils', () => {
  it.each([
    [0, 1],
    [0.4, 1],
    [320.7, 320],
    [500, 500],
    [1920, MAX_BUCKET_COUNT],
  ])('bucketCountForWidth_Width%f_Gives%i', (width, expected) => {
    expect(bucketCountForWidth(width)).toBe(expected);
  });

  it.each([
    [ChartRange.Hour, '2026-01-01T11:00:00Z'],
    [ChartRange.SixHours, '2026-01-01T06:00:00Z'],
    [ChartRange.Day, '2025-12-31T12:00:00Z'],
  ])('rangeBounds_%s_StartsBeforeNowByTheRangeLength', (range, from) => {
    const now = Temporal.Instant.from('2026-01-01T12:00:00Z');

    expect(rangeBounds(now, range)).toEqual({ from, to: '2026-01-01T12:00:00Z' });
  });

  it('customBounds_LocalFromToInTheZone_GivesTheUtcInstants', () => {
    const range = {
      from: Temporal.PlainDateTime.from('2026-01-15T10:00'),
      to: Temporal.PlainDateTime.from('2026-01-15T12:30'),
    };

    expect(customBounds(range, KYIV)).toEqual({ from: '2026-01-15T08:00:00Z', to: '2026-01-15T10:30:00Z' });
  });

  it.each([
    ['no end', { from: Temporal.PlainDateTime.from('2026-01-15T10:00'), to: null }],
    [
      'an end before the start',
      {
        from: Temporal.PlainDateTime.from('2026-01-15T12:00'),
        to: Temporal.PlainDateTime.from('2026-01-15T10:00'),
      },
    ],
  ])('customBounds_With_%s_GivesNull', (_name, range) => {
    expect(customBounds(range, KYIV)).toBeNull();
  });

  it('toPlainRange_CustomSelection_GivesTheLocalMomentsAndAPresetGivesNone', () => {
    const custom = toPlainRange({ kind: 'custom', from: '2026-01-15T08:00:00Z', to: '2026-01-15T10:30:00Z' }, KYIV);
    const preset = toPlainRange({ kind: 'preset', preset: ChartRange.Day }, KYIV);

    expect(custom.from?.toString()).toBe('2026-01-15T10:00:00');
    expect(custom.to?.toString()).toBe('2026-01-15T12:30:00');
    expect(preset).toEqual({ from: null, to: null });
  });

  it('selectionBounds_PresetAndCustom_GiveTheWindowOfEach', () => {
    const now = Temporal.Instant.from('2026-01-01T12:00:00Z');

    expect(selectionBounds(now, { kind: 'preset', preset: ChartRange.Hour })).toEqual({
      from: '2026-01-01T11:00:00Z',
      to: '2026-01-01T12:00:00Z',
    });
    expect(selectionBounds(now, { kind: 'custom', from: 'a', to: 'b' })).toEqual({ from: 'a', to: 'b' });
  });

  describe('zoomedBounds', () => {
    const buckets = ['10:00', '10:01', '10:02', '10:03', '10:04'].map((time) => ({
      start: `2026-01-01T${time}:00Z`,
    }));

    it('zoomedBounds_ViewOfTheMiddleBuckets_RunsFromTheFirstStartToTheEndOfTheLastBucket', () => {
      expect(zoomedBounds(buckets, 1, 3)).toEqual({ from: '2026-01-01T10:01:00Z', to: '2026-01-01T10:04:00Z' });
    });

    it('zoomedBounds_IndexesOutsideTheBuckets_StayInsideThem', () => {
      expect(zoomedBounds(buckets, -2.4, 9)).toEqual({ from: '2026-01-01T10:00:00Z', to: '2026-01-01T10:05:00Z' });
    });

    it('zoomedBounds_FractionalIndexes_RoundToTheNearestBucket', () => {
      expect(zoomedBounds(buckets, 0.6, 2.4)).toEqual({ from: '2026-01-01T10:01:00Z', to: '2026-01-01T10:03:00Z' });
    });

    it('zoomedBounds_WithLessThanTwoBuckets_GivesNull', () => {
      expect(zoomedBounds(buckets.slice(0, 1), 0, 0)).toBeNull();
    });
  });

  it('chartableMetrics_MixedKinds_KeepsOnlyTheNumericOnes', () => {
    const metrics = [
      { id: 'a', valueKind: MetricValueKind.Numeric },
      { id: 'b', valueKind: MetricValueKind.Boolean },
      { id: 'c', valueKind: MetricValueKind.StateSet },
    ] as MetricDefinition[];

    expect(chartableMetrics(metrics).map((metric) => metric.id)).toEqual(['a']);
  });
});
