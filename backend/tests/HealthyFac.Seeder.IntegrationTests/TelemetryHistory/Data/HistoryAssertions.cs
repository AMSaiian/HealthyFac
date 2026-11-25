using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Agents.Tools.Writes;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;
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

    /// <summary>
    /// Checks the work order of each drain and its link: the work order is completed, the signals that were open 1
    /// minute before the maintenance are linked to it, and the case has 1 completed planner run with 1 committed
    /// link action. No other case has a planner run.
    /// </summary>
    /// <param name="expectation">The moments of the script.</param>
    /// <param name="machineIds">The machine IDs by key.</param>
    /// <param name="signals">The signals of every case.</param>
    /// <param name="runs">Every agent run.</param>
    /// <param name="actions">Every agent action.</param>
    /// <param name="orders">Every work order, with its history.</param>
    public static void AssertDrainWorkOrders(
        HistoryExpectation expectation,
        IReadOnlyDictionary<string, Guid> machineIds,
        IReadOnlyList<MachineSignal> signals,
        IReadOnlyList<AgentRun> runs,
        IReadOnlyList<AgentAction> actions,
        IReadOnlyList<WorkOrder> orders
    )
    {
        (string Machine, string Maintenance, string Title)[] drains =
        [
            (TelemetryHistorySeedTestData.Lathe, expectation.LatheMaintenanceFrom,
                TelemetryHistorySeedTestData.LatheWorkOrderTitle),
            (TelemetryHistorySeedTestData.Pump, expectation.PumpMaintenanceFrom,
                TelemetryHistorySeedTestData.PumpWorkOrderTitle),
        ];
        var plannerRuns = runs.Where(run => run.Role == AgentRole.Planner).ToList();
        plannerRuns.Should().HaveCount(drains.Length);
        foreach (var (machine, maintenance, title) in drains)
        {
            var link = At(maintenance).AddMinutes(value: -1);
            var order = orders.Single(candidate => candidate.Title == title);
            var openAtLink = signals.Where(signal => signal.MachineId == machineIds[machine]
                    && signal.OpenedAt <= link
                    && (signal.ResolvedAt is null || signal.ResolvedAt > link))
                .ToList();
            var caseId = openAtLink.Select(signal => signal.CaseId).Distinct().Should().ContainSingle().Subject;
            var run = plannerRuns.Where(candidate => candidate.CaseId == caseId).Should().ContainSingle().Subject;
            var action = actions.Where(candidate => candidate.RunId == run.Id).Should().ContainSingle().Subject;
            var payload = ActionBuffering.Deserialize<LinkWorkOrderPayload>(action);

            order.Current.Status.Should().Be(WorkOrderStatus.Completed, machine);
            openAtLink.Should()
                .OnlyContain(
                    signal => signal.Coverage == SignalCoverage.Linked && signal.WorkOrderId == order.Id, machine);
            run.State.Should().Be(AgentRunState.Completed, machine);
            action.Kind.Should().Be(AgentActionKind.LinkWorkOrder, machine);
            action.State.Should().Be(AgentActionState.Committed, machine);
            payload.WorkOrderId.Should().Be(order.Id, machine);
            payload.SignalIds.Should().BeEquivalentTo(openAtLink.Select(signal => signal.Id), machine);
        }
    }
}
