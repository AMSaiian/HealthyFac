using HealthyFac.Seeder.IntegrationTests.Cases.Data;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.Machines.Data;
using HealthyFac.Seeder.IntegrationTests.People.Data;
using HealthyFac.Seeder.IntegrationTests.TelemetryHistory.Data;
using HealthyFac.Seeder.IntegrationTests.Timeline.Data;
using HealthyFac.Seeder.IntegrationTests.WorkOrders.Data;

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
        var broken = await _fixture.CountBrokenMessagesAsync();
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
        broken.WithErrors.Should().Be(expected: 0);
        broken.WithoutSchemaOrPayload.Should().Be(expected: 0);
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

    private async Task<JobResult> RunTelemetryJobAsync() =>
        await _fixture.RunJobAsync(
            [TelemetryHistorySeedTestData.TelemetryArgument],
            TestContext.Current.CancellationToken,
            TelemetryHistorySeedTestData.CreateBaseDirectory(TelemetryHistorySeedTestData.TestScript),
            anchor: TelemetryHistorySeedTestData.ThursdayAnchor);
}
