using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Seeder.IntegrationTests.People.Data;
using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.IntegrationTests.TelemetryHistory.Data;

/// <summary>The checks of the history that the test script and the committed script share.</summary>
public static class HistoryAssertions
{
    private static readonly TimeSpan RuleDelay = TimeSpan.FromMinutes(minutes: 5);
    private static readonly TimeSpan SilenceLead = TimeSpan.FromSeconds(seconds: 119);
    private static readonly TimeSpan ModeLead = TimeSpan.FromSeconds(seconds: 60);

    public static DateTime At(string moment) =>
        SeedCalendar.ToUtc(
                RelativeMoment.Parse(moment),
                TelemetryHistorySeedTestData.ThursdayAnchor,
                TimeZoneInfo.FindSystemTimeZoneById(PeopleTestData.ZoneId))
            .UtcDateTime;

    public static void AssertSamples(
        HistoryExpectation expectation,
        IReadOnlyDictionary<string, Guid> machineIds,
        IReadOnlyDictionary<Guid, int> counts,
        (DateTime First, DateTime Last) range
    )
    {
        counts[machineIds[TelemetryHistorySeedTestData.Lathe]].Should().Be(expectation.SampleCount);
        counts[machineIds[TelemetryHistorySeedTestData.Compressor]].Should().Be(expectation.SampleCount);
        counts[machineIds[TelemetryHistorySeedTestData.Pump]]
            .Should()
            .Be(expectation.SampleCount - expectation.GapSampleCount);
        range.First.Should().Be(At($"h-{expectation.WindowHours}"));
        range.Last.Should().Be(At(moment: "h-0"));
    }

    public static void AssertEpisodes(
        HistoryExpectation expectation,
        IReadOnlyDictionary<string, Guid> machineIds,
        IReadOnlyList<EpisodeRow> episodes,
        TimeSpan evaluationStep
    )
    {
        var lathe = machineIds[TelemetryHistorySeedTestData.Lathe];
        var pump = machineIds[TelemetryHistorySeedTestData.Pump];
        var compressor = machineIds[TelemetryHistorySeedTestData.Compressor];
        foreach (var (machine, from, maintenanceStart, maintenanceEnd) in new[]
            {
                (lathe, At(expectation.LatheDrainFrom), At(expectation.LatheMaintenanceFrom),
                    At(expectation.LatheMaintenanceUntil)),
                (pump, At(expectation.PumpDrainFrom), At(expectation.PumpMaintenanceFrom),
                    At(expectation.PumpMaintenanceUntil)),
            })
        {
            foreach (var rule in TelemetryHistorySeedTestData.DrainRules)
            {
                episodes.Should()
                    .Contain(
                        episode => episode.MachineId == machine
                            && episode.Rule == rule
                            && episode.OpenedAt > from
                            && episode.OpenedAt < maintenanceStart
                            && episode.ResolvedAt >= maintenanceEnd,
                        rule);
            }

            episodes.Where(episode => episode.MachineId == machine
                    && TelemetryHistorySeedTestData.DrainRules.Contains(episode.Rule))
                .Should()
                .OnlyContain(episode => episode.Severity != TelemetryHistorySeedTestData.Alarm);
        }

        var stepEnd = At(expectation.CompressorStepUntil);
        episodes.Should()
            .Contain(episode => episode.MachineId == compressor
                && episode.Rule == TelemetryHistorySeedTestData.Drift
                && episode.MetricKey == TelemetryHistorySeedTestData.CompressorStepMetric
                && episode.OpenedAt > At(expectation.CompressorStepFrom)
                && episode.OpenedAt < stepEnd
                && episode.ResolvedAt > stepEnd
                && episode.ResolvedAt <= stepEnd + RuleDelay);
        var gapEnd = At(expectation.PumpGapUntil);
        episodes.Should()
            .Contain(episode => episode.MachineId == pump
                && episode.Rule == TelemetryHistorySeedTestData.SilentMachine
                && episode.OpenedAt >= At(expectation.PumpGapFrom) + SilenceLead
                && episode.OpenedAt <= At(expectation.PumpGapFrom) + SilenceLead + evaluationStep
                && episode.ResolvedAt >= gapEnd
                && episode.ResolvedAt <= gapEnd + RuleDelay);
        var overrideEnd = At(expectation.LatheOverrideUntil);
        episodes.Should()
            .Contain(episode => episode.MachineId == lathe
                && episode.Rule == TelemetryHistorySeedTestData.UnknownMode
                && episode.OpenedAt >= At(expectation.LatheOverrideFrom) + ModeLead
                && episode.OpenedAt <= overrideEnd
                && episode.ResolvedAt >= overrideEnd
                && episode.ResolvedAt <= overrideEnd + RuleDelay);
        episodes.Should().OnlyContain(episode => episode.State == TelemetryHistorySeedTestData.Resolved);
    }

    public static void AssertCases(
        HistoryExpectation expectation,
        IReadOnlyDictionary<string, Guid> machineIds,
        IReadOnlyList<MaintenanceCase> cases,
        IReadOnlyList<MachineSignal> signals
    )
    {
        var resolved = cases.Where(maintenanceCase => maintenanceCase.State == CaseState.Resolved).ToList();
        var open = cases.Where(maintenanceCase => maintenanceCase.State == CaseState.Open).ToList();
        resolved.GroupBy(maintenanceCase => maintenanceCase.MachineId)
            .ToDictionary(group => group.Key, group => group.Count())
            .Should()
            .BeEquivalentTo(
                new Dictionary<Guid, int>
                {
                    [machineIds[TelemetryHistorySeedTestData.Lathe]] = 2,
                    [machineIds[TelemetryHistorySeedTestData.Pump]] = 2,
                    [machineIds[TelemetryHistorySeedTestData.Compressor]] = 1,
                });
        open.Should().ContainSingle().Which.MachineId.Should().Be(machineIds[TelemetryHistorySeedTestData.Lathe]);
        foreach (var maintenanceCase in resolved)
        {
            var own = signals.Where(signal => signal.CaseId == maintenanceCase.Id).ToList();
            var lastResolved = own.Max(signal => signal.ResolvedAt!.Value);
            own.Should().NotBeEmpty().And.OnlyContain(signal => signal.MachineId == maintenanceCase.MachineId);
            own.Should().OnlyContain(signal => signal.ResolvedAt != null);
            maintenanceCase.OpenedAt.Should().Be(own.Min(signal => signal.OpenedAt));
            maintenanceCase.OpenedAt.Should()
                .BeOnOrAfter(At($"h-{expectation.WindowHours}"))
                .And.BeOnOrBefore(At(moment: "h-0"));
            maintenanceCase.ClosedAt.Should().BeOnOrAfter(lastResolved).And.BeOnOrBefore(lastResolved + RuleDelay);
        }
    }
}
