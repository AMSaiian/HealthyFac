using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Agents.Tools.Writes;
using HealthyFac.Insights.Application.Cases;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Seeder.IntegrationTests.Cases.Data;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.Machines.Data;
using HealthyFac.Seeder.IntegrationTests.People.Data;
using HealthyFac.Seeder.IntegrationTests.TelemetryHistory.Data;
using HealthyFac.Seeder.IntegrationTests.Timeline.Data;
using HealthyFac.Seeder.IntegrationTests.WorkOrders.Data;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.TelemetryHistory;

namespace HealthyFac.Seeder.IntegrationTests.TelemetryHistory;

[Collection(SeederCollection.Name)]
public sealed class TelemetryHistorySeedTests(SeederFixture fixture)
{
    private readonly SeederFixture _fixture = fixture;

    [Fact(Explicit = true)]
    public async Task RunAsync_WithTheTestScript_StoresTheSamplesTheMarkerAndTheMaintenance()
    {
        var expectation = TelemetryHistorySeedTestData.TestExpectation;

        var job = await RunTelemetryJobAsync();

        var machineIds = await _fixture.LoadMachineIdsByKeyAsync();
        var lathe = machineIds[TelemetryHistorySeedTestData.Lathe];
        var pump = machineIds[TelemetryHistorySeedTestData.Pump];
        var counts = await _fixture.CountMessagesByMachineAsync();
        var range = await _fixture.FindSampleRangeAsync();

        var gapCount = await _fixture.CountMessagesInRangeAsync(
            pump, At(expectation.PumpGapFrom), At(expectation.PumpGapUntil));

        var (withErrors, withoutSchemaOrPayload) = await _fixture.CountBrokenMessagesAsync();
        var hasMarker = await _fixture.HasReceivedMessageAsync(TelemetryHistorySeedTestData.MarkerMessageId);
        var latheStates = await _fixture.LoadStateRowsAsync(lathe);
        var pumpStates = await _fixture.LoadStateRowsAsync(pump);

        var latheSpeedInMaintenance = await _fixture.LoadNumericValuesAsync(
            lathe,
            TelemetryHistorySeedTestData.SpindleSpeed,
            At(expectation.LatheMaintenanceFrom).AddSeconds(value: 5),
            At(expectation.LatheMaintenanceUntil));

        var latheSpeedBefore = await _fixture.LoadNumericValuesAsync(
            lathe,
            TelemetryHistorySeedTestData.SpindleSpeed,
            At(TelemetryHistorySeedTestData.LatheSpeedBeforeFrom),
            At(TelemetryHistorySeedTestData.LatheSpeedBeforeUntil));

        var pumpSpeedInMaintenance = await _fixture.LoadNumericValuesAsync(
            pump,
            TelemetryHistorySeedTestData.PumpSpeed,
            At(expectation.PumpMaintenanceFrom).AddSeconds(value: 5),
            At(expectation.PumpMaintenanceUntil));

        var modeOne = await LoadModeOneCountsAsync();

        job.ExitCode.Should().Be(expected: 0);
        HistoryAssertions.AssertSamples(expectation, machineIds, counts, range);
        gapCount.Should().Be(expected: 0);
        withErrors.Should().Be(expected: 0);
        withoutSchemaOrPayload.Should().Be(expected: 0);
        hasMarker.Should().BeTrue();

        HasState(latheStates, TelemetryHistorySeedTestData.InMaintenance, At(expectation.LatheMaintenanceFrom))
            .Should()
            .BeTrue();

        HasState(latheStates, TelemetryHistorySeedTestData.Operational, At(expectation.LatheMaintenanceUntil))
            .Should()
            .BeTrue();

        HasState(pumpStates, TelemetryHistorySeedTestData.InMaintenance, At(expectation.PumpMaintenanceFrom))
            .Should()
            .BeTrue();

        HasState(pumpStates, TelemetryHistorySeedTestData.Operational, At(expectation.PumpMaintenanceUntil))
            .Should()
            .BeTrue();

        latheSpeedInMaintenance.Should().NotBeEmpty().And.OnlyContain(value => value == 0);
        latheSpeedBefore.Should().NotBeEmpty().And.Contain(value => value > 0);
        pumpSpeedInMaintenance.Should().NotBeEmpty().And.OnlyContain(value => value == 0);
        modeOne.Should().BeEquivalentTo(ModeOneCounts.Expected);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithTheTestScript_OpensAndResolvesTheEpisodesOfEachRuleInItsWindow()
    {
        await RunTelemetryJobAsync();

        var machineIds = await _fixture.LoadMachineIdsByKeyAsync();
        var episodes = await _fixture.LoadEpisodesAsync();
        var step = await _fixture.ReadEvaluationStepAsync();

        HistoryAssertions.AssertEpisodes(TelemetryHistorySeedTestData.TestExpectation, machineIds, episodes, step);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithTheTestScript_GivesOneResolvedCaseForEachEventAndKeepsTheFakeCaseOpen()
    {
        await RunTelemetryJobAsync();

        var machineIds = await _fixture.LoadMachineIdsByKeyAsync();
        var (cases, signals) = await _fixture.LoadCasesAsync();

        HistoryAssertions.AssertCases(TelemetryHistorySeedTestData.TestExpectation, machineIds, cases, signals);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_AfterARunCancelledAtAStep_ExitsWithOneAndLeavesNoMarker()
    {
        using var stop = new CancellationTokenSource();
        var baseDirectory = TelemetryHistorySeedTestData.CreateBaseDirectory(TelemetryHistorySeedTestData.TestScript);
        var log = new CancellingWriter(TelemetryHistorySeedTestData.CancelItem, stop);

        var stopped = await _fixture.RunJobAsync(
            [TelemetryHistorySeedTestData.TelemetryArgument],
            stop.Token,
            baseDirectory,
            log,
            TelemetryHistorySeedTestData.ThursdayAnchor);

        var next = await _fixture.RunJobAsync(
            [TelemetryHistorySeedTestData.TelemetryArgument],
            TestContext.Current.CancellationToken,
            baseDirectory,
            anchor: TelemetryHistorySeedTestData.ThursdayAnchor);

        var hasMarker = await _fixture.HasReceivedMessageAsync(TelemetryHistorySeedTestData.MarkerMessageId);

        stopped.ExitCode.Should().Be(expected: 1);
        next.ExitCode.Should().Be(expected: 1);
        next.Output.Should().Contain(TelemetryHistorySeedTestData.ResetTelemetryHint);
        hasMarker.Should().BeFalse();
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithAWorkOrderOnADrain_StoresTheWorkOrderWithItsMomentsAndStatuses()
    {
        var job = await RunTelemetryJobAsync(TelemetryHistorySeedTestData.TestScriptWithWorkOrder);

        var orders = await _fixture.LoadWorkOrdersAsync();
        var employees = await _fixture.LoadEmployeesAsync();
        var order = orders.Single(candidate => candidate.Title == TelemetryHistorySeedTestData.DrainWorkOrderTitle);
        var history = order.History.OrderBy(row => row.Revision).ToList();
        var assignee = employees.Single(employee => employee.Id == order.Current.AssigneeId);

        var supervisor = employees.Single(employee =>
            employee.Email.StartsWith(TelemetryHistorySeedTestData.WorkOrderCreator));

        job.ExitCode.Should().Be(expected: 0);
        history.Select(row => row.Status).Should().Equal(WorkOrdersTestData.PumpSealStatuses);

        history.Select(row => row.CreatedAt)
            .Should()
            .BeEquivalentTo(
                TelemetryHistorySeedTestData.DrainWorkOrderMoments.Select(At),
                options => options.WithStrictOrdering()
                    .Using<DateTime>(context => context.Subject.Should()
                        .BeCloseTo(context.Expectation, TelemetryHistorySeedTestData.StateTolerance))
                    .WhenTypeIs<DateTime>());

        assignee.Email.Should().StartWith(TelemetryHistorySeedTestData.MaintenanceActor);
        order.Current.Estimate.Should().Be(TimeSpan.FromMinutes(minutes: 60));
        order.Current.PlannedStartUtc.Should().Be(TelemetryHistorySeedTestData.DrainPlannedStartUtc);

        history.Take(count: 2)
            .Select(row => row.CreatedBy)
            .Should()
            .OnlyContain(userId => userId == supervisor.IdentityUserId);

        order.CreatedBy.Should().Be(supervisor.IdentityUserId!.Value);
    }

    [Fact(Explicit = true)]
    public async Task Run_WithAnEmptyTitleOnADrainWorkOrder_FailsWithTheCreateItemAndTheCoreRule()
    {
        var baseDirectory = TelemetryHistorySeedTestData.CreateBaseDirectory(
            TelemetryHistorySeedTestData.TestScriptWithEmptyTitle);

        var failures = await _fixture.CheckTelemetrySeedAsync(
            baseDirectory, TelemetryHistorySeedTestData.ThursdayAnchor, TestContext.Current.CancellationToken);

        failures.Should()
            .ContainSingle(failure => failure.Item == TelemetryHistorySeedTestData.CreateWorkOrderItem
                && failure.Rule.Contains("title"));
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithABusySlotOfTheAssignee_PlansTheDrainWorkOrderAfterIt()
    {
        var job = await _fixture.RunJobAsync(
            [TelemetryHistorySeedTestData.TelemetryArgument],
            TestContext.Current.CancellationToken,
            TelemetryHistorySeedTestData.CreateBaseDirectory(
                TelemetryHistorySeedTestData.TestScriptWithWorkOrder, TelemetryHistorySeedTestData.BusyWorkOrder),
            anchor: TelemetryHistorySeedTestData.ThursdayAnchor);

        var orders = await _fixture.LoadWorkOrdersAsync();
        var busy = orders.Single(order => order.Title == TelemetryHistorySeedTestData.BusyWorkOrderTitle);
        var drain = orders.Single(order => order.Title == TelemetryHistorySeedTestData.DrainWorkOrderTitle);
        job.ExitCode.Should().Be(expected: 0);
        drain.Current.PlannedStartUtc.Should().Be(busy.Current.PlannedStartUtc + busy.Current.Estimate);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithAWorkOrderOnADrain_LinksTheSignalsThatWereOpenAtTheLinkMoment()
    {
        await RunTelemetryJobAsync(TelemetryHistorySeedTestData.TestScriptWithWorkOrder);

        var machineIds = await _fixture.LoadMachineIdsByKeyAsync();
        var (cases, signals) = await _fixture.LoadCasesAsync();
        var (runs, actions) = await _fixture.LoadAgentRunsAsync();
        var orders = await _fixture.LoadWorkOrdersAsync();
        var order = orders.Single(candidate => candidate.Title == TelemetryHistorySeedTestData.DrainWorkOrderTitle);
        var overrideEnd = At(TelemetryHistorySeedTestData.TestExpectation.LatheOverrideUntil);

        var drainCase = cases.Single(candidate => candidate.MachineId == machineIds[TelemetryHistorySeedTestData.Lathe]
            && candidate.State == CaseState.Resolved
            && candidate.OpenedAt > overrideEnd);

        var caseSignals = signals.Where(signal => signal.CaseId == drainCase.Id).ToList();
        var link = At(TelemetryHistorySeedTestData.LinkMoment);

        var openAtLink = caseSignals.Where(signal => signal.OpenedAt <= link
                && (signal.ResolvedAt is null || signal.ResolvedAt > link))
            .ToList();

        var plannerRuns = runs.Where(run => run.CaseId == drainCase.Id && run.Role == AgentRole.Planner).ToList();
        var linkActions = actions.Where(action => plannerRuns.Select(run => run.Id).Contains(action.RunId)).ToList();
        var action = linkActions.Should().ContainSingle().Subject;
        var payload = ActionBuffering.Deserialize<LinkWorkOrderPayload>(action);
        var outcome = await _fixture.LoadCaseOutcomeAsync(drainCase.Id, TestContext.Current.CancellationToken);

        var toolCalls = await _fixture.LoadToolCallsAsync(
            drainCase.Id, plannerRuns.Single().Id, TestContext.Current.CancellationToken);

        openAtLink.Should().NotBeEmpty();

        openAtLink.Should()
            .OnlyContain(signal => signal.Coverage == SignalCoverage.Linked && signal.WorkOrderId == order.Id);

        caseSignals.Except(openAtLink).Should().OnlyContain(signal => signal.Coverage != SignalCoverage.Linked);
        plannerRuns.Should().ContainSingle().Which.State.Should().Be(AgentRunState.Completed);
        action.Kind.Should().Be(AgentActionKind.LinkWorkOrder);
        action.State.Should().Be(AgentActionState.Committed);
        payload.WorkOrderId.Should().Be(order.Id);
        payload.SignalIds.Should().BeEquivalentTo(openAtLink.Select(signal => signal.Id));
        var toolCall = toolCalls.Should().ContainSingle().Subject;
        var arguments = _fixture.DecodeLinkArguments(toolCall.Arguments);
        toolCall.Sequence.Should().Be(expected: 1);
        toolCall.ToolName.Should().Be(LinkWorkOrderToolRequest.ToolName);
        toolCall.Outcome.Should().Be(ToolCallOutcome.Succeeded);
        arguments.WorkOrderId.Should().Be(order.Id);
        arguments.EpisodeIds.Should().BeEquivalentTo(openAtLink.Select(signal => signal.Id));
        outcome.Kind.Should().Be(CaseOutcomeKind.LinkedWorkOrder);
        outcome.WorkOrderId.Should().Be(order.Id);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_OnAMachineWithoutAnOpenCase_FailsTheLinkWithTheItemAndWritesNoRun()
    {
        await _fixture.RunFullSeedAsync(
            TestContext.Current.CancellationToken, TelemetryHistorySeedTestData.ThursdayAnchor);

        var machineIds = await _fixture.LoadMachineIdsByKeyAsync();
        var orders = await _fixture.LoadWorkOrdersAsync();

        var failure = await Record.ExceptionAsync(() => DrainCaseLink.RunAsync(
            _fixture.Hosts.Insights,
            TelemetryHistorySeedTestData.PumpLinkItem,
            TelemetryHistorySeedTestData.PumpDrain,
            PeopleTestData.CompanyId,
            machineIds[TelemetryHistorySeedTestData.Pump],
            orders[0].Id,
            TestContext.Current.CancellationToken));

        var (runs, _) = await _fixture.LoadAgentRunsAsync();

        failure.Should()
            .BeOfType<SeedFileException>()
            .Which.Message.Should()
            .Contain(TelemetryHistorySeedTestData.PumpLinkItem)
            .And.Contain(expected: "no open case");

        runs.Should().NotContain(run => run.Role == AgentRole.Planner);
    }

    private static bool HasState(IEnumerable<StateRow> rows, string state, DateTime at) =>
        rows.Any(row => row.State == state
            && (row.StartedAt - at).Duration() < TelemetryHistorySeedTestData.StateTolerance);

    private static DateTime At(string moment) => HistoryAssertions.At(moment);

    private async Task<ModeOneCounts> LoadModeOneCountsAsync()
    {
        var employees = await _fixture.LoadEmployeesAsync();
        var invites = await _fixture.LoadInvitesAsync();
        var templateCount = await _fixture.CountShiftTemplateVersionsAsync();
        var overrides = await _fixture.LoadOverridesAsync();
        var machines = await _fixture.LoadMachinesAsync();
        var drafts = await _fixture.LoadMachineDraftsAsync();
        var workOrders = await _fixture.LoadWorkOrdersAsync();
        var workOrderDraft = await _fixture.LoadWorkOrderDraftAsync();

        return new ModeOneCounts
        {
            Employees = employees.Count,
            AcceptedInvites = invites.Count(invite => invite.AcceptedAt is not null),
            OpenInvites = invites.Count(invite => invite.AcceptedAt is null),
            Templates = templateCount,
            Overrides = overrides.Count,
            Machines = machines.Count,
            MachineDrafts = drafts.Count,
            WorkOrders = workOrders.Count,
            WorkOrderDraftId = workOrderDraft.Id,
        };
    }

    private async Task<JobResult> RunTelemetryJobAsync(string script = TelemetryHistorySeedTestData.TestScript) =>
        await _fixture.RunJobAsync(
            [TelemetryHistorySeedTestData.TelemetryArgument],
            TestContext.Current.CancellationToken,
            TelemetryHistorySeedTestData.CreateBaseDirectory(script),
            anchor: TelemetryHistorySeedTestData.ThursdayAnchor);
}
