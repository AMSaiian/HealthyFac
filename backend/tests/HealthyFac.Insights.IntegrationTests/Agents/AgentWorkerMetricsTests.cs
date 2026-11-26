using System.Diagnostics.Metrics;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.IntegrationTests.Agents.Data;
using HealthyFac.Insights.IntegrationTests.Cases;
using HealthyFac.Insights.IntegrationTests.Cases.Data;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Constants = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Agents;

namespace HealthyFac.Insights.IntegrationTests.Agents;

[Collection(SignalCaseCollection.Name)]
[Feature(TestFeatures.Cases)]
public sealed class AgentWorkerMetricsTests(SignalCaseFixture fixture)
{
    private readonly SignalCaseFixture _fixture = fixture;

    [Fact]
    public async Task Run_Completed_CountsOneRunWithItsRoleAndResultAndRecordsOneDuration()
    {
        var runs = Collect<long>(Constants.Instruments.Runs);
        var duration = Collect<double>(Constants.Instruments.RunDuration);
        await ArrangeQueuedRunAsync(AgentRole.Triage);
        _fixture.Agents.TriageChat.Script(TriageTestData.Answer(verdict: "watch"));

        await _fixture.RunAgentWorkerOnceAsync();

        var measurement = runs.GetMeasurementSnapshot().Should().ContainSingle().Subject;
        measurement.Value.Should().Be(expected: 1);
        measurement.Tags[Constants.Tags.Role].Should().Be(expected: "triage");
        measurement.Tags[Constants.Tags.Result].Should().Be(Constants.Results.Completed);
        var durationMeasurement = duration.GetMeasurementSnapshot().Should().ContainSingle().Subject;
        durationMeasurement.Tags[Constants.Tags.Role].Should().Be(expected: "triage");
        duration.Instrument!.Unit.Should().Be(expected: "s");
        runs.Instrument!.Unit.Should().Be(expected: "{run}");
    }

    [Fact]
    public async Task Run_Failed_CountsOneRunWithTheFailedResult()
    {
        var runs = Collect<long>(Constants.Instruments.Runs);
        await ArrangeQueuedRunAsync(AgentRole.Screener);

        await _fixture.RunAgentWorkerOnceAsync();

        var measurement = runs.GetMeasurementSnapshot().Should().ContainSingle().Subject;
        measurement.Tags[Constants.Tags.Role].Should().Be(expected: "screener");
        measurement.Tags[Constants.Tags.Result].Should().Be(Constants.Results.Failed);
    }

    [Fact]
    public async Task Sweep_OfAnExpiredRun_CountsOneInterruptedRunWithoutADuration()
    {
        var runs = Collect<long>(Constants.Instruments.Runs);
        var duration = Collect<double>(Constants.Instruments.RunDuration);
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.ClearRunsAsync();
        await _fixture.AddRunningRunAsync((await _fixture.LoadCasesAsync()).Single().Id);
        _fixture.Time.Advance(TimeSpan.FromSeconds(AgentTestData.PastLeaseSeconds));

        await _fixture.RunAgentWorkerOnceAsync();

        var measurement = runs.GetMeasurementSnapshot().Should().ContainSingle().Subject;
        measurement.Tags[Constants.Tags.Role].Should().Be(expected: "triage");
        measurement.Tags[Constants.Tags.Result].Should().Be(Constants.Results.Interrupted);
        duration.GetMeasurementSnapshot().Should().BeEmpty();
    }

    [Fact]
    public async Task Round_WithTwoDueRuns_ReportsTheRunThatStaysQueued()
    {
        var queued = Collect<long>(Constants.Instruments.QueuedRuns);
        await ArrangeQueuedRunAsync(AgentRole.Screener);
        await _fixture.AddQueuedRunAsync(AgentRole.Screener, (await _fixture.LoadCasesAsync()).Single().Id);

        await _fixture.RunAgentWorkerOnceAsync();
        queued.RecordObservableInstruments();

        queued.LastMeasurement!.Value.Should().Be(expected: 1);
        queued.Instrument!.Unit.Should().Be(expected: "{run}");
    }

    private async Task ArrangeQueuedRunAsync(AgentRole role)
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.ClearRunsAsync();
        await _fixture.AddQueuedRunAsync(role, (await _fixture.LoadCasesAsync()).Single().Id);
    }

    private MetricCollector<T> Collect<T>(string instrument)
        where T : struct =>
        new(_fixture.Inner.GetSingleton<IMeterFactory>(), Constants.Source, instrument);
}
