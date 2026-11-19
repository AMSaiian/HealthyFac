import { type SignalEpisode, SignalEpisodeState, SignalSeverity } from '@machines/signals/signal-episode';
import {
  type ChartColors,
  ChartGroup,
  type ChartInput,
  DEFAULT_HIDDEN_GROUPS,
  episodeBucketRange,
  groupIndexes,
  isLegendEntry,
  limitsOf,
  toChartData,
  toggledGroups,
  withAlpha,
} from '@machines/signals/metric-chart/metric-chart.utils';

const COLORS: ChartColors = { avg: 'avg', band: 'band', warning: 'warning', alarm: 'alarm', muted: 'muted' };

const BUCKETS = [
  { start: '2026-01-01T10:00:00Z', avg: 10, min: 8, max: 12, count: 5 },
  { start: '2026-01-01T10:01:00Z', avg: 20, min: 18, max: 22, count: 5 },
  { start: '2026-01-01T10:02:00Z', avg: 30, min: 28, max: 32, count: 5 },
];

const LABELS: ChartInput['labels'] = {
  avg: 'Average',
  range: 'Range',
  warningLimits: 'Warning limits',
  alarmLimits: 'Alarm limits',
  signals: 'Signals',
};

function episode(severity: SignalSeverity, openedAt: string, resolvedAt: string | null): SignalEpisode {
  return {
    episodeId: `${severity}-${openedAt}`,
    rule: 'threshold',
    severity,
    state: resolvedAt === null ? SignalEpisodeState.Open : SignalEpisodeState.Resolved,
    metricDefinitionId: 'm1',
    metricKey: 'pump.vibration',
    statusDefinitionId: null,
    statusCode: null,
    phaseCode: null,
    openedAt,
    resolvedAt,
  };
}

function input(overrides: Partial<ChartInput>): ChartInput {
  return {
    buckets: BUCKETS,
    limits: { warningLow: null, warningHigh: null, alarmLow: null, alarmHigh: null },
    episodes: [],
    colors: COLORS,
    labels: LABELS,
    hiddenGroups: DEFAULT_HIDDEN_GROUPS,
    formatTime: (iso) => iso,
    episodeText: (item) => `${item.rule}|${item.severity}|${item.openedAt}`,
    ...overrides,
  };
}

const ALL_LIMITS = { warningLow: 1, warningHigh: 25, alarmLow: 0, alarmHigh: 30 };

describe('metric chart', () => {
  it('episodeBucketRange_EpisodeInsideTheSecondBucket_GivesOnlyThatBucket', () => {
    const result = episodeBucketRange(BUCKETS, {
      openedAt: '2026-01-01T10:01:10Z',
      resolvedAt: '2026-01-01T10:01:40Z',
    });

    expect(result).toEqual({ first: 1, last: 1 });
  });

  it('episodeBucketRange_OpenEpisode_RunsFromItsStartToTheLastBucket', () => {
    const result = episodeBucketRange(BUCKETS, { openedAt: '2026-01-01T10:01:30Z', resolvedAt: null });

    expect(result).toEqual({ first: 1, last: 2 });
  });

  it('episodeBucketRange_EpisodeResolvedAtTheStartOfABucket_LeavesThatBucketOut', () => {
    const result = episodeBucketRange(BUCKETS, {
      openedAt: '2026-01-01T09:00:00Z',
      resolvedAt: '2026-01-01T10:01:00Z',
    });

    expect(result).toEqual({ first: 0, last: 0 });
  });

  it('episodeBucketRange_EpisodeBeforeTheRange_GivesNull', () => {
    const result = episodeBucketRange(BUCKETS, {
      openedAt: '2026-01-01T08:00:00Z',
      resolvedAt: '2026-01-01T09:00:00Z',
    });

    expect(result).toBeNull();
  });

  it('toChartData_WithAllLimitsAndAnEpisode_HasFiveLegendEntries', () => {
    const result = toChartData(
      input({
        limits: ALL_LIMITS,
        episodes: [episode(SignalSeverity.Alarm, '2026-01-01T10:01:00Z', null)],
      }),
    );

    const entries = result.datasets.filter(isLegendEntry).map((dataset) => dataset.label);
    expect(entries).toEqual(['Range', 'Average', 'Warning limits', 'Alarm limits', 'Signals']);
  });

  it('toChartData_Always_HidesTheWarningLimitsAtFirstAndShowsTheOtherGroups', () => {
    const result = toChartData(input({ limits: ALL_LIMITS }));

    const hiddenGroups = new Set(result.datasets.filter((dataset) => dataset['hidden'] === true).map((d) => d.hfGroup));
    expect(hiddenGroups).toEqual(new Set([ChartGroup.WarningLimits]));
  });

  it('toChartData_SomeLimits_AddsOneDashedLinePerSetLimitInItsGroup', () => {
    const result = toChartData(input({ limits: { warningLow: null, warningHigh: 25, alarmLow: 5, alarmHigh: null } }));

    const lines = result.datasets.filter((set) => set['borderDash'] !== undefined);
    expect(lines.map((line) => [line.hfGroup, line.data, line['borderColor']])).toEqual([
      [ChartGroup.WarningLimits, [25, 25, 25], 'warning'],
      [ChartGroup.AlarmLimits, [5, 5, 5], 'alarm'],
    ]);
  });

  it('toChartData_NoLimits_HasNoLimitLine', () => {
    const lines = toChartData(input({})).datasets.filter((set) => set['borderDash'] !== undefined);

    expect(lines).toEqual([]);
  });

  it('toChartData_Buckets_BuildsTheAverageLineAndTheBandWithoutOwnLines', () => {
    const result = toChartData(input({}));

    const average = result.datasets.find((set) => set.hfGroup === ChartGroup.Avg);
    const band = result.datasets.filter((set) => set.hfGroup === ChartGroup.Range);
    expect(average?.data).toEqual([10, 20, 30]);
    expect(band.map((set) => set.data)).toEqual([
      [12, 22, 32],
      [8, 18, 28],
    ]);
    expect(band.map((set) => set['borderWidth'])).toEqual([0, 0]);
    expect(band[1]['fill']).toBe('-1');
    expect(result.labels).toEqual(BUCKETS.map((bucket) => bucket.start));
  });

  it('toChartData_TwoEpisodes_MakesOneStripSegmentForEachWithItsTooltipAndSeverityColor', () => {
    const warning = episode(SignalSeverity.Warning, '2026-01-01T10:00:00Z', '2026-01-01T10:01:00Z');
    const alarm = episode(SignalSeverity.Alarm, '2026-01-01T10:02:00Z', null);

    const result = toChartData(input({ episodes: [warning, alarm] }));

    const segments = result.datasets.filter((set) => set.hfTooltip !== undefined);
    expect(segments.map((set) => [set.hfTooltip, set['borderColor']])).toEqual([
      ['threshold|warning|2026-01-01T10:00:00Z', 'warning'],
      ['threshold|alarm|2026-01-01T10:02:00Z', 'alarm'],
    ]);
    expect(segments.map((set) => set.data.map((value) => value !== null))).toEqual([
      [true, false, false],
      [false, false, true],
    ]);
    expect(segments.every((set) => !isLegendEntry(set) && set.hfGroup === ChartGroup.Signals)).toBe(true);
  });

  it('toChartData_EpisodeOutsideTheBuckets_MakesNoSegment', () => {
    const outside = episode(SignalSeverity.Alarm, '2026-01-01T07:00:00Z', '2026-01-01T08:00:00Z');

    const result = toChartData(input({ episodes: [outside] }));

    expect(result.datasets.filter((set) => set.hfTooltip !== undefined)).toEqual([]);
  });

  it('toChartData_AfterAClickOnTheWarningLimits_ShowsThemAgain', () => {
    const hiddenGroups = toggledGroups(DEFAULT_HIDDEN_GROUPS, ChartGroup.WarningLimits);

    const result = toChartData(input({ limits: ALL_LIMITS, hiddenGroups }));

    expect(result.datasets.some((dataset) => dataset['hidden'] === true)).toBe(false);
  });

  it('toggledGroups_GroupThatWasShown_IsHiddenAndTheOriginalSetStays', () => {
    const next = toggledGroups(new Set<string>(), ChartGroup.Avg);

    expect(next).toEqual(new Set([ChartGroup.Avg]));
  });

  it('groupIndexes_ForADatasetOfAGroup_GivesEveryDatasetOfTheGroup', () => {
    const result = toChartData(input({ limits: ALL_LIMITS }));

    const warningIndexes = result.datasets.flatMap((set, index) =>
      set.hfGroup === ChartGroup.WarningLimits ? [index] : [],
    );
    expect(groupIndexes(result.datasets, warningIndexes[0])).toEqual(warningIndexes);
    expect(warningIndexes).toHaveLength(2);
  });

  it('limitsOf_DefinitionWithoutLimitFields_GivesNoLimits', () => {
    expect(limitsOf({})).toEqual({ warningLow: null, warningHigh: null, alarmLow: null, alarmHigh: null });
    expect(limitsOf(null)).toEqual({ warningLow: null, warningHigh: null, alarmLow: null, alarmHigh: null });
  });

  it('withAlpha_LongHexColor_GivesAnRgbaColor', () => {
    expect(withAlpha('#ff8000', 0.5)).toBe('rgba(255, 128, 0, 0.5)');
  });

  it('withAlpha_RgbColor_GivesAnRgbaColor', () => {
    expect(withAlpha('rgb(255, 128, 0)', 0.5)).toBe('rgba(255, 128, 0, 0.5)');
  });

  it('withAlpha_ColorThatIsNotLongHex_StaysAsItIs', () => {
    expect(withAlpha('light-dark(#111111, #222222)', 0.5)).toBe('light-dark(#111111, #222222)');
  });
});
