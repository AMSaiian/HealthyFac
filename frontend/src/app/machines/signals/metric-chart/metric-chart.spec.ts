import { type SignalEpisode, SignalEpisodeState, SignalSeverity } from '@machines/signals/signal-episode';
import {
  type ChartColors,
  ChartGroup,
  type ChartInput,
  DEFAULT_HIDDEN_GROUPS,
  episodeBucketRange,
  groupIndexes,
  hasTooltipLines,
  isLegendEntry,
  isTooltipEntry,
  limitsOf,
  scaleRange,
  signalAreas,
  tickLabel,
  timeTicks,
  toChartData,
  toggledGroups,
  tooltipLabel,
  tooltipSwatch,
  withAlpha,
  zoomLimits,
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
    viewEnd: END,
    episodeText: (item) => `${item.rule}|${item.severity}|${item.openedAt}`,
    ...overrides,
  };
}

const ys = (set: { readonly data: readonly { readonly y: number | null }[] }): (number | null)[] =>
  set.data.map((point) => point.y);

const END = Date.parse('2026-01-01T10:03:00Z');
const at = (iso: string): number => Date.parse(iso);

const ALL_LIMITS = { warningLow: 1, warningHigh: 25, alarmLow: 0, alarmHigh: 30 };

describe('metric chart', { tags: ['Evaluation'] }, () => {
  it.each([
    [
      'EpisodeInsideTheSecondBucket_GivesOnlyThatBucket',
      '2026-01-01T10:01:10Z',
      '2026-01-01T10:01:40Z',
      { first: 1, last: 1 },
    ],
    ['OpenEpisode_RunsFromItsStartToTheLastBucket', '2026-01-01T10:01:30Z', null, { first: 1, last: 2 }],
    [
      'EpisodeResolvedAtTheStartOfABucket_LeavesThatBucketOut',
      '2026-01-01T09:00:00Z',
      '2026-01-01T10:01:00Z',
      { first: 0, last: 0 },
    ],
    ['EpisodeBeforeTheRange_GivesNull', '2026-01-01T08:00:00Z', '2026-01-01T09:00:00Z', null],
  ])('episodeBucketRange_%s', (_name, openedAt, resolvedAt, expected) => {
    const result = episodeBucketRange(BUCKETS, { openedAt, resolvedAt });

    expect(result).toEqual(expected);
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
    expect(lines.map((line) => [line.hfGroup, ys(line), line['borderColor']])).toEqual([
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
    expect(ys(average!)).toEqual([10, 20, 30]);
    expect(band.map(ys)).toEqual([
      [12, 22, 32],
      [8, 18, 28],
    ]);
    expect(band.map((set) => set['borderWidth'])).toEqual([0, 0]);
    expect(band[1]['fill']).toBe('-1');
  });

  it('toChartData_Buckets_PutsEachPointAtTheEpochMillisecondsOfItsBucketStart', () => {
    const result = toChartData(input({ limits: ALL_LIMITS }));

    const starts = BUCKETS.map((bucket) => Date.parse(bucket.start));
    const average = result.datasets.find((set) => set.hfGroup === ChartGroup.Avg);
    const alarmLine = result.datasets.find((set) => set.hfGroup === ChartGroup.AlarmLimits);
    expect(average?.data.map((point) => point.x)).toEqual(starts);
    expect(alarmLine?.data.map((point) => point.x)).toEqual(starts);
  });

  it('scaleRange_MinAndMaxBetweenTwoBuckets_GivesTheExactInstantsWithoutRounding', () => {
    const min = Date.parse('2026-01-01T10:00:17.250Z');
    const max = Date.parse('2026-01-01T10:01:42.500Z');

    const result = scaleRange(min, max);

    expect(result).toEqual({ from: '2026-01-01T10:00:17.25Z', to: '2026-01-01T10:01:42.5Z' });
  });

  it('zoomLimits_BoundsOfOneHourWith60Buckets_KeepsTheBoundsAndAMinimumOfTwoBuckets', () => {
    const result = zoomLimits({ from: '2026-01-01T10:00:00Z', to: '2026-01-01T11:00:00Z' }, 60);

    expect(result).toEqual({
      min: Date.parse('2026-01-01T10:00:00Z'),
      max: Date.parse('2026-01-01T11:00:00Z'),
      minRange: 2 * 60_000,
    });
  });

  it('zoomLimits_WithoutBuckets_StillKeepsAMinimumRangeOfTwoBucketsOfOne', () => {
    const result = zoomLimits({ from: '2026-01-01T10:00:00Z', to: '2026-01-01T10:01:00Z' }, 0);

    expect(result.minRange).toBe(2 * 60_000);
  });

  it('tickLabel_ValueInTheZone_GivesTheLocalDayMonthHourAndMinute', () => {
    const value = Date.parse('2026-01-15T08:30:00Z');

    const label = tickLabel(value, 'Europe/Kyiv', 3_600_000);

    expect(label).toContain('10:30');
    expect(label).toContain('15');
  });

  it('tickLabel_ViewOfLessThanTenMinutes_AddsTheSeconds', () => {
    const value = Date.parse('2026-01-15T08:30:00Z');

    const longView = tickLabel(value, 'Europe/Kyiv', 3_600_000);
    const shortView = tickLabel(value, 'Europe/Kyiv', 60_000);

    expect(shortView).toContain('10:30:00');
    expect(longView).not.toContain('10:30:00');
  });

  it('toChartData_TwoOverlappingEpisodes_MakesOneAreaFilledToTheAxisWithTheTintOfTheHighestSeverity', () => {
    const warning = episode(SignalSeverity.Warning, '2026-01-01T10:00:10Z', '2026-01-01T10:01:30Z');
    const alarm = episode(SignalSeverity.Alarm, '2026-01-01T10:01:00Z', '2026-01-01T10:01:50Z');
    const colors = { ...COLORS, alarm: 'rgb(255, 0, 0)' };

    const result = toChartData(input({ episodes: [warning, alarm], colors }));

    const areas = result.datasets.filter((set) => set.hfSignals !== undefined);
    expect(areas).toHaveLength(1);
    expect(ys(areas[0])).toEqual([1, 1, 1]);
    expect(areas[0]['fill']).toBe('origin');
    expect(areas[0]['borderWidth']).toBe(0);
    expect(areas[0]['backgroundColor']).toBe('rgba(255, 0, 0, 0.18)');
    expect(areas[0].hfGroup).toBe(ChartGroup.Signals);
    expect(isLegendEntry(areas[0])).toBe(false);
  });

  it('toChartData_OpenEpisode_MakesAnAreaToTheEndOfTheView', () => {
    const open = episode(SignalSeverity.Warning, '2026-01-01T10:01:10Z', null);

    const result = toChartData(input({ episodes: [open] }));

    const areas = result.datasets.filter((set) => set.hfSignals !== undefined);
    expect(areas.map(ys)).toEqual([[null, 1, 1, 1]]);
  });

  it('toChartData_EpisodeInsideOneBucket_ClosesTheAreaAtTheStartOfTheNextBucket', () => {
    const inside = episode(SignalSeverity.Alarm, '2026-01-01T10:01:10Z', '2026-01-01T10:01:40Z');

    const result = toChartData(input({ episodes: [inside] }));

    const area = result.datasets.find((set) => set.hfSignals !== undefined);
    expect(area?.data).toEqual([
      { x: at('2026-01-01T10:00:00Z'), y: null },
      { x: at('2026-01-01T10:01:00Z'), y: 1 },
      { x: at('2026-01-01T10:02:00Z'), y: 1 },
    ]);
  });

  it('toChartData_EpisodeInTheLastBucket_ClosesTheAreaAtTheEndOfTheView', () => {
    const last = episode(SignalSeverity.Alarm, '2026-01-01T10:02:10Z', '2026-01-01T10:02:40Z');

    const result = toChartData(input({ episodes: [last] }));

    const area = result.datasets.find((set) => set.hfSignals !== undefined);
    expect(area?.data.slice(-2)).toEqual([
      { x: at('2026-01-01T10:02:00Z'), y: 1 },
      { x: END, y: 1 },
    ]);
  });

  it('toChartData_EpisodeOutsideTheBuckets_MakesNoArea', () => {
    const outside = episode(SignalSeverity.Alarm, '2026-01-01T07:00:00Z', '2026-01-01T08:00:00Z');

    const result = toChartData(input({ episodes: [outside] }));

    expect(result.datasets.filter((set) => set.hfSignals !== undefined)).toEqual([]);
  });

  it('tooltipLabel_AreaDataset_ListsExactlyTheSignalsThatCoverTheBucket', () => {
    const warning = episode(SignalSeverity.Warning, '2026-01-01T10:00:10Z', '2026-01-01T10:01:30Z');
    const alarm = episode(SignalSeverity.Alarm, '2026-01-01T10:01:00Z', '2026-01-01T10:01:50Z');
    const datasets = toChartData(input({ episodes: [warning, alarm] })).datasets;
    const area = datasets.findIndex((set) => set.hfSignals !== undefined);

    const first = tooltipLabel(datasets, area, 0, String);
    const second = tooltipLabel(datasets, area, 1, String);
    const outside = tooltipLabel(datasets, area, 2, String);

    expect(first).toEqual(['threshold|warning|2026-01-01T10:00:10Z']);
    expect(second).toEqual(['threshold|warning|2026-01-01T10:00:10Z', 'threshold|alarm|2026-01-01T10:01:00Z']);
    expect(outside).toEqual([]);
  });

  it('signalAreas_TwoOverlappingSignals_GivesOneAreaWithBothAndTheHighestSeverity', () => {
    const warning = episode(SignalSeverity.Warning, '2026-01-01T10:00:00Z', '2026-01-01T10:03:00Z');
    const alarm = episode(SignalSeverity.Alarm, '2026-01-01T10:02:00Z', '2026-01-01T10:05:00Z');

    const areas = signalAreas([alarm, warning], END);

    expect(areas).toEqual([
      {
        from: at('2026-01-01T10:00:00Z'),
        to: at('2026-01-01T10:05:00Z'),
        severity: SignalSeverity.Alarm,
        episodes: [warning, alarm],
      },
    ]);
  });

  it('signalAreas_TwoTouchingSignals_GivesOneArea', () => {
    const first = episode(SignalSeverity.Warning, '2026-01-01T10:00:00Z', '2026-01-01T10:02:00Z');
    const second = episode(SignalSeverity.Warning, '2026-01-01T10:02:00Z', '2026-01-01T10:04:00Z');

    const areas = signalAreas([first, second], END);

    expect(areas.map((area) => [area.from, area.to])).toEqual([
      [at('2026-01-01T10:00:00Z'), at('2026-01-01T10:04:00Z')],
    ]);
  });

  it('signalAreas_TwoSignalsWithAGap_GivesTwoAreas', () => {
    const first = episode(SignalSeverity.Warning, '2026-01-01T10:00:00Z', '2026-01-01T10:01:00Z');
    const second = episode(SignalSeverity.Alarm, '2026-01-01T10:02:00Z', '2026-01-01T10:03:00Z');

    const areas = signalAreas([first, second], END);

    expect(areas.map((area) => [area.severity, area.episodes.length])).toEqual([
      [SignalSeverity.Warning, 1],
      [SignalSeverity.Alarm, 1],
    ]);
  });

  it('signalAreas_OpenSignal_EndsAtTheEndOfTheView', () => {
    const open = episode(SignalSeverity.Warning, '2026-01-01T10:01:00Z', null);

    const areas = signalAreas([open], END);

    expect(areas.map((area) => area.to)).toEqual([END]);
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

  it.each([
    ['LongHexColor_GivesAnRgbaColor', '#ff8000', 'rgba(255, 128, 0, 0.5)'],
    ['RgbColor_GivesAnRgbaColor', 'rgb(255, 128, 0)', 'rgba(255, 128, 0, 0.5)'],
    ['ColorThatIsNotLongHex_StaysAsItIs', 'light-dark(#111111, #222222)', 'light-dark(#111111, #222222)'],
  ])('withAlpha_%s', (_name, color, expected) => {
    expect(withAlpha(color, 0.5)).toBe(expected);
  });

  it('tooltipLabel_RangeBand_GivesTheMinimumAndTheMaximumInOneLine', () => {
    const { datasets } = toChartData(input({}));
    const lower = datasets.findIndex((dataset) => dataset.hfGroup === ChartGroup.Range && isTooltipEntry(dataset));

    expect(tooltipLabel(datasets, lower, 1, String)).toBe('Range: 18 – 22');
  });

  it('isTooltipEntry_UpperEdgeOfTheRangeBand_IsFalse', () => {
    const { datasets } = toChartData(input({}));

    expect(datasets.filter((dataset) => dataset.hfGroup === ChartGroup.Range).map(isTooltipEntry)).toEqual([
      false,
      true,
    ]);
  });

  it('tooltipLabel_AverageLine_GivesTheLabelAndTheValue', () => {
    const { datasets } = toChartData(input({}));
    const average = datasets.findIndex((dataset) => dataset.hfGroup === ChartGroup.Avg);

    expect(tooltipLabel(datasets, average, 2, String)).toBe('Average: 30');
  });

  it.each([
    [
      'DatasetWithAWideLine_GivesASolidBoxInTheLineColorWithoutAFrame',
      { borderColor: 'warning', backgroundColor: 'muted', borderWidth: 6 },
      'warning',
    ],
    ['DatasetWithoutALine_GivesItsFillColor', { backgroundColor: 'band', borderWidth: 0 }, 'band'],
    [
      'DatasetWithATranslucentFill_GivesAnOpaqueBox',
      { backgroundColor: 'rgba(255, 128, 0, 0.18)', borderWidth: 0 },
      'rgb(255, 128, 0)',
    ],
  ])('tooltipSwatch_%s', (_name, dataset, color) => {
    expect(tooltipSwatch(dataset)).toEqual({ borderColor: color, backgroundColor: color, borderWidth: 0 });
  });
});

describe('metric chart time axis', { tags: ['Evaluation'] }, () => {
  const ZONE = 'Europe/Kyiv';

  it.each([
    [
      'ViewOfOneHour_PutsTheInnerTicksOnLocalTenMinutes',
      '2026-01-15T10:00:17Z',
      '2026-01-15T11:00:17Z',
      [
        '2026-01-15T10:10:00Z',
        '2026-01-15T10:20:00Z',
        '2026-01-15T10:30:00Z',
        '2026-01-15T10:40:00Z',
        '2026-01-15T10:50:00Z',
      ],
    ],
    [
      'ViewOf90Seconds_PutsTheInnerTicksOnLocalThirtySeconds',
      '2026-01-15T10:00:07Z',
      '2026-01-15T10:01:37Z',
      ['2026-01-15T10:00:30Z', '2026-01-15T10:01:00Z', '2026-01-15T10:01:30Z'],
    ],
    [
      'ViewOfOneDay_PutsTheInnerTicksOnLocalSixHours',
      '2026-01-14T22:00:00Z',
      '2026-01-15T22:00:00Z',
      ['2026-01-15T04:00:00Z', '2026-01-15T10:00:00Z', '2026-01-15T16:00:00Z'],
    ],
    [
      'ViewOfManyDays_PutsTheInnerTicksOnLocalMidnights',
      '2026-01-01T10:00:00Z',
      '2026-01-06T10:00:00Z',
      [
        '2026-01-01T22:00:00Z',
        '2026-01-02T22:00:00Z',
        '2026-01-03T22:00:00Z',
        '2026-01-04T22:00:00Z',
        '2026-01-05T22:00:00Z',
      ],
    ],
  ])('timeTicks_%s', (_name, from, to, inner) => {
    const min = at(from);
    const max = at(to);

    const ticks = timeTicks(min, max, ZONE, 8);

    expect(ticks).toEqual([min, ...inner.map(at), max]);
  });

  it('timeTicks_ViewOf365Days_KeepsTheTickCountWithinTheLimitAndKeepsTheEnds', () => {
    const min = at('2026-01-01T00:00:00Z');
    const max = at('2027-01-01T00:00:00Z');

    const ticks = timeTicks(min, max, ZONE, 8);

    expect(ticks.length).toBeLessThanOrEqual(8);
    expect(ticks[0]).toBe(min);
    expect(ticks.at(-1)).toBe(max);
  });

  it('tickLabel_TimeThatIsNotOnAWholeMinute_ShowsTheSecondsInALongView', () => {
    const odd = tickLabel(at('2026-01-15T08:30:17Z'), ZONE, 3_600_000);
    const whole = tickLabel(at('2026-01-15T08:30:00Z'), ZONE, 3_600_000);

    expect(odd).toContain('10:30:17');
    expect(whole).not.toContain('10:30:00');
  });
});

describe('metric chart tooltip lines', { tags: ['Evaluation'] }, () => {
  it('hasTooltipLines_ClosingPointOfAnArea_IsFalseAndABucketOfASignalIsTrue', () => {
    const inside = episode(SignalSeverity.Alarm, '2026-01-01T10:01:10Z', '2026-01-01T10:01:40Z');
    const area = toChartData(input({ episodes: [inside] })).datasets.find((set) => set.hfSignals !== undefined)!;

    expect([hasTooltipLines(area, 1), hasTooltipLines(area, 2)]).toEqual([true, false]);
    expect(hasTooltipLines({}, 2)).toBe(true);
  });
});
