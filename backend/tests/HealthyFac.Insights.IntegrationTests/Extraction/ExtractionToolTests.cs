using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.IntegrationTests.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Insights.IntegrationTests.Documents.Data;
using HealthyFac.Insights.IntegrationTests.Extraction.Data;
using HealthyFac.Insights.IntegrationTests.Ingest.Data;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Testing.Features;
using static HealthyFac.Insights.IntegrationTests.Extraction.Data.ExtractionToolTestData;

namespace HealthyFac.Insights.IntegrationTests.Extraction;

[Collection(InsightsCollection.Name)]
[Feature(TestFeatures.Ingest)]
public sealed class ExtractionToolTests(InsightsFixture fixture)
{
    private const string Final = "Done.";
    private const int OverMachineLimit = DraftMessageLimits.MaxMachines + 1;

    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task Run_WithTheRecordedCallsOfTheDemoDocument_ProposesTheMachineMetricStatusAndPhase()
    {
        _fixture.ArrangeFile();
        await _fixture.StartAttemptAsync();
        var chat = _fixture.Ingest.Chat;
        chat.ScriptToolCall(callId: "c1", functionName: "propose_machine", MachineCall);

        chat.ScriptToolCalls(
            (CallId: "c2", Name: "propose_metric", ArgumentsJson: MetricCall),
            (CallId: "c3", Name: "propose_status", ArgumentsJson: StatusCall));

        chat.ScriptToolCall(callId: "c4", functionName: "propose_operating_phase", PhaseCall);
        chat.ScriptText(Final);

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var drafts = await _fixture.ReadOutboxMessagesAsync<DraftsProposed>();
        var machine = drafts.Should().ContainSingle().Which.Machines.Should().ContainSingle().Subject;
        machine.Name.Should().Be(expected: "CNC Mill VMC-500");
        var metric = machine.Metrics.Should().ContainSingle().Subject;
        metric.Key.Should().Be(expected: "spindle.temperature");
        metric.Unit.Should().Be(expected: "celsius");
        metric.WarningHigh.Should().Be(expected: 70);
        metric.AlarmHigh.Should().Be(expected: 80);
        metric.Behavior.Should().Be(expected: "level");
        var status = machine.Statuses.Should().ContainSingle().Subject;
        status.Code.Should().Be(expected: "E104");
        var phase = machine.Phases.Should().ContainSingle().Subject;
        phase.Code.Should().Be(expected: "cutting");
        phase.Conditions.Select(condition => condition.MetricDraftId).Should().Equal(metric.DraftId, null);
        phase.Conditions.Select(condition => condition.StatusDraftId).Should().Equal(null, status.DraftId);
        phase.Limits.Should().ContainSingle().Which.MetricDraftId.Should().Be(metric.DraftId);
    }

    [Fact]
    public async Task Run_WithAnInvalidCallThenACorrectedCall_KeepsOnlyTheCorrectedMetric()
    {
        _fixture.ArrangeFile();
        await _fixture.StartAttemptAsync();
        var chat = _fixture.Ingest.Chat;
        chat.ScriptToolCall(callId: "c1", functionName: "propose_machine", MachineCall);
        chat.ScriptToolCall(callId: "c2", functionName: "propose_metric", InvalidKeyMetricCall);
        chat.ScriptToolCall(callId: "c3", functionName: "propose_metric", CorrectedMetricCall);
        chat.ScriptText(Final);

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var drafts = await _fixture.ReadOutboxMessagesAsync<DraftsProposed>();
        var calls = await _fixture.LoadExtractorToolCallsAsync();

        drafts.Should()
            .ContainSingle()
            .Which.Machines.Single()
            .Metrics.Select(metric => metric.Key)
            .Should()
            .Equal("coolant.level");

        calls.Select(call => call.Outcome)
            .Should()
            .Equal(ToolCallOutcome.Succeeded, ToolCallOutcome.Rejected, ToolCallOutcome.Succeeded);
    }

    [Fact]
    public async Task Run_WithInvalidCallsInARow_FailsTheAttemptSendsNothingAndLogsTheFailedRun()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        _fixture.Ingest.Chat.ScriptToolCall(callId: "c1", functionName: "propose_machine", MachineCall);
        _fixture.Ingest.Chat.ScriptInvalidToolCalls(count: 3);

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var attempt = await _fixture.LoadAttemptAsync(attemptId);
        var drafts = await _fixture.ReadOutboxMessagesAsync<DraftsProposed>();
        var runs = await _fixture.LoadExtractorRunsAsync();
        var calls = await _fixture.LoadExtractorToolCallsAsync();
        attempt.State.Should().Be(IngestState.Failed);
        attempt.ErrorCode.Should().Be(IngestErrorCode.InvalidExtraction);
        drafts.Should().BeEmpty();
        var run = runs.Should().ContainSingle().Subject;
        run.State.Should().Be(AgentRunState.Failed);
        run.IngestAttemptId.Should().Be(attemptId);
        calls.Should().HaveCount(expected: 4).And.OnlyContain(call => call.RunId == run.Id);
    }

    [Fact]
    public async Task Run_WithMoreMachinesThanTheLimit_RejectsTheCallAboveItAndKeepsTheLimit()
    {
        _fixture.ArrangeFile();
        await _fixture.StartAttemptAsync();

        _fixture.Ingest.Chat.ScriptToolCalls(
        [
            .. Enumerable.Range(start: 1, OverMachineLimit)
                .Select(number => (CallId: $"c{number}", Name: "propose_machine",
                    ArgumentsJson: $$"""{"name":"M{{number}}"}""")),
        ]);

        _fixture.Ingest.Chat.ScriptText(Final);

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var drafts = await _fixture.ReadOutboxMessagesAsync<DraftsProposed>();
        var calls = await _fixture.LoadExtractorToolCallsAsync();
        drafts.Should().ContainSingle().Which.Machines.Should().HaveCount(DraftMessageLimits.MaxMachines);
        calls[^1].Outcome.Should().Be(ToolCallOutcome.Rejected);
        calls[^1].Result.Should().Contain(DraftMessageLimits.MaxMachines.ToString());
    }

    [Fact]
    public async Task Run_WithAPhaseOfAMachineFile_ResolvesKeysOfTheMachineToDefinitionIds()
    {
        _fixture.ArrangeFile(machineId: FileMachineId);
        _fixture.Ingest.Signals.Signals = ExistingSignals;
        await _fixture.StartAttemptAsync();
        var chat = _fixture.Ingest.Chat;
        chat.ScriptToolCall(callId: "c1", functionName: "propose_machine", MachineCall);
        chat.ScriptToolCall(callId: "c2", functionName: "propose_operating_phase", PhaseOfExistingCall);
        chat.ScriptText(Final);

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var drafts = await _fixture.ReadOutboxMessagesAsync<DraftsProposed>();
        var machine = drafts.Should().ContainSingle().Which.Machines.Single();
        var phase = machine.Phases.Should().ContainSingle().Subject;
        machine.TargetMachineId.Should().Be(FileMachineId);
        phase.Conditions.Select(condition => condition.MetricId).Should().Equal(ExistingMetricId, null);
        phase.Conditions.Select(condition => condition.StatusId).Should().Equal(null, ExistingStatusId);

        phase.Conditions.Should()
            .OnlyContain(condition => condition.MetricDraftId == null && condition.StatusDraftId == null);
    }

    [Fact]
    public async Task Run_WithAPhaseThatNamesAnUnknownKey_RejectsTheCallToTheModel()
    {
        _fixture.ArrangeFile();
        await _fixture.StartAttemptAsync();
        var chat = _fixture.Ingest.Chat;
        chat.ScriptToolCall(callId: "c1", functionName: "propose_machine", MachineCall);
        chat.ScriptToolCall(callId: "c2", functionName: "propose_operating_phase", PhaseWithUnknownKeyCall);
        chat.ScriptText(Final);

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var drafts = await _fixture.ReadOutboxMessagesAsync<DraftsProposed>();
        var calls = await _fixture.LoadExtractorToolCallsAsync();
        drafts.Should().ContainSingle().Which.Machines.Single().Phases.Should().BeEmpty();
        calls[^1].Outcome.Should().Be(ToolCallOutcome.Rejected);
        calls[^1].Result.Should().Contain(expected: "no.such.key");
    }

    [Fact]
    public async Task Run_WhenTheAttemptCompletes_StoresTheExtractorRunWithItsToolCallsAndTokens()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();

        _fixture.Ingest.Chat.Usage = new Microsoft.Extensions.AI.UsageDetails
            { InputTokenCount = 11, OutputTokenCount = 7 };

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var runs = await _fixture.LoadExtractorRunsAsync();
        var calls = await _fixture.LoadExtractorToolCallsAsync();
        var run = runs.Should().ContainSingle().Subject;
        run.State.Should().Be(AgentRunState.Completed);
        run.IngestAttemptId.Should().Be(attemptId);
        run.PromptVersion.Should().Be(expected: "extractor-v4");
        run.InputTokens.Should().BeGreaterThan(expected: 0);

        calls.Select(call => call.ToolName)
            .Should()
            .Equal("propose_machine", "propose_metric", "propose_status");
    }

    [Fact]
    public async Task Run_WhenTheProviderStaysUnavailable_FailsTheAttemptAndSendsNoProposal()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();

        _fixture.Ingest.Chat.Failure = new HttpRequestException(
            message: "unavailable", inner: null, System.Net.HttpStatusCode.BadRequest);

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var attempt = await _fixture.LoadAttemptAsync(attemptId);
        var drafts = await _fixture.ReadOutboxMessagesAsync<DraftsProposed>();
        var runs = await _fixture.LoadExtractorRunsAsync();
        attempt.State.Should().Be(IngestState.Failed);
        drafts.Should().BeEmpty();
        runs.Should().ContainSingle().Which.State.Should().Be(AgentRunState.Failed);
    }
}
