using HealthyFac.Insights.Application.Agents.Tools.Reads;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Insights.IntegrationTests.Agents.Data;
using HealthyFac.Insights.IntegrationTests.Cases;
using HealthyFac.Insights.IntegrationTests.Cases.Data;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Insights.IntegrationTests.Agents;

[Collection(SignalCaseCollection.Name)]
public sealed class AgentWorkerTests(SignalCaseFixture fixture)
{
    private readonly SignalCaseFixture _fixture = fixture;

    [Fact]
    public async Task Sweep_WithAnExpiredLease_InterruptsTheRunDiscardsItsActionsAndQueuesNoReplacement()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.ClearRunsAsync();
        var caseId = (await _fixture.LoadCasesAsync()).Single().Id;
        var runId = await _fixture.AddRunningRunAsync(caseId);
        await _fixture.AddBufferedActionAsync(runId);
        _fixture.Time.Advance(TimeSpan.FromSeconds(AgentTestData.PastLeaseSeconds));

        var isInterrupted = await _fixture.SweepAgentRunsAsync();
        var claimed = await _fixture.ClaimAgentRunAsync();

        var runs = await _fixture.LoadRunsAsync();
        var actions = await _fixture.LoadActionsAsync();
        isInterrupted.Should().BeTrue();
        claimed.Should().BeNull();
        var run = runs.Should().ContainSingle().Subject;
        run.State.Should().Be(AgentRunState.Interrupted);
        actions.Should().ContainSingle().Which.State.Should().Be(AgentActionState.Discarded);
    }

    [Fact]
    public async Task Renew_AfterEachToolCall_KeepsTheRunOutOfTheSweep()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.ClearRunsAsync();
        var caseId = (await _fixture.LoadCasesAsync()).Single().Id;
        var runId = await _fixture.AddRunningRunAsync(caseId);
        _fixture.Time.Advance(TimeSpan.FromSeconds(AgentTestData.HalfLeaseSeconds));
        await _fixture.CallToolAsync(ListOpenWorkOrdersToolRequest.ToolName, existingRunId: runId);
        _fixture.Time.Advance(TimeSpan.FromSeconds(AgentTestData.HalfLeaseSeconds));

        var isInterrupted = await _fixture.SweepAgentRunsAsync();

        var run = await _fixture.LoadRunAsync(runId);
        isInterrupted.Should().BeFalse();
        run.State.Should().Be(AgentRunState.Running);
    }

    [Fact]
    public async Task Run_WhenTheSweepInterruptsItDuringTheModelCall_StopsAtTheRenewalAndCommitsNothing()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        _fixture.Agents.TriageChat.Script(TriageTestData.Answer(verdict: "dismiss"));
        _fixture.Time.Advance(TimeSpan.FromSeconds(CaseTestData.DebounceSeconds));
        _fixture.Agents.TriageChat.Before = async () =>
        {
            _fixture.Time.Advance(TimeSpan.FromSeconds(AgentTestData.PastLeaseSeconds));
            await _fixture.SweepAgentRunsAsync();
        };

        var didRun = await _fixture.RunAgentWorkerOnceAsync();

        var run = (await _fixture.LoadRunsAsync()).Single();
        var signal = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        var maintenanceCase = (await _fixture.LoadCasesAsync()).Single();
        didRun.Should().BeTrue();
        run.State.Should().Be(AgentRunState.Interrupted);
        run.Verdict.Should().BeNull();
        signal.Coverage.Should().Be(SignalCoverage.New);
        maintenanceCase.State.Should().Be(CaseState.Open);
        _fixture.Inner.Logs.Entries.Should().NotContain(entry => entry.Level >= LogLevel.Warning);
    }

    [Fact]
    public async Task Sweep_WithALeaseThatHoldsOn_InterruptsNothing()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.ClearRunsAsync();
        var caseId = (await _fixture.LoadCasesAsync()).Single().Id;
        var runId = await _fixture.AddRunningRunAsync(caseId);

        var isInterrupted = await _fixture.SweepAgentRunsAsync();

        var run = await _fixture.LoadRunAsync(runId);
        isInterrupted.Should().BeFalse();
        run.State.Should().Be(AgentRunState.Running);
    }

    [Fact]
    public async Task Claim_WithARunThatIsNotDueYet_ClaimsNothing()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));

        var claimed = await _fixture.ClaimAgentRunAsync();

        claimed.Should().BeNull();
    }

    [Fact]
    public async Task Run_WithAClosedCase_SkipsWithTheCaseNotOpenReasonAndMakesNoModelCall()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        var caseId = (await _fixture.LoadCasesAsync()).Single().Id;
        await _fixture.CloseCaseAsync(caseId);
        _fixture.Time.Advance(TimeSpan.FromSeconds(CaseTestData.DebounceSeconds));

        var didRun = await _fixture.RunAgentWorkerOnceAsync();

        var run = (await _fixture.LoadRunsAsync()).Single();
        didRun.Should().BeTrue();
        run.State.Should().Be(AgentRunState.Skipped);
        run.SkipReason.Should().Be(AgentRunSkipReason.CaseNotOpen);
        run.Model.Should().BeNull();
        run.InputTokens.Should().Be(expected: 0);
        _fixture.Inner.Ingest.Chat.CallCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task Run_WithOnlyADraftedSignal_SkipsWithTheNothingToReviewReasonAndMakesNoModelCall()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.DraftSignalAsync(CaseTestData.FirstEpisodeId);
        _fixture.Time.Advance(TimeSpan.FromSeconds(CaseTestData.DebounceSeconds));

        var didRun = await _fixture.RunAgentWorkerOnceAsync();

        var run = (await _fixture.LoadRunsAsync()).Single();
        didRun.Should().BeTrue();
        run.State.Should().Be(AgentRunState.Skipped);
        run.SkipReason.Should().Be(AgentRunSkipReason.NothingToReview);
        _fixture.Inner.Ingest.Chat.CallCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task Run_WithAnUncoveredOpenSignalAndNoRunnerOfTheRole_FailsWithTheUnsupportedRoleCode()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.ClearRunsAsync();
        await _fixture.AddQueuedRunAsync(AgentRole.Extractor, (await _fixture.LoadCasesAsync()).Single().Id);

        var didRun = await _fixture.RunAgentWorkerOnceAsync();

        var run = (await _fixture.LoadRunsAsync()).Single();
        didRun.Should().BeTrue();
        run.State.Should().Be(AgentRunState.Failed);
        run.ErrorCode.Should().Be(AgentRunErrorCode.UnexpectedError);
    }

    [Fact]
    public async Task Run_WhenTheRunnerThrows_FailsTheRunDiscardsItsActionsAndQueuesNoReplacement()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.ClearRunsAsync();
        await _fixture.AddQueuedRunAsync(AgentRole.Screener, (await _fixture.LoadCasesAsync()).Single().Id);

        var didRun = await _fixture.RunAgentWorkerOnceAsync();
        _fixture.Time.Advance(TimeSpan.FromSeconds(AgentTestData.PastLeaseSeconds));
        var isInterrupted = await _fixture.SweepAgentRunsAsync();

        var runs = await _fixture.LoadRunsAsync();
        var actions = await _fixture.LoadActionsAsync();
        didRun.Should().BeTrue();
        isInterrupted.Should().BeFalse();
        var run = runs.Should().ContainSingle().Subject;
        run.State.Should().Be(AgentRunState.Failed);
        run.ErrorCode.Should().Be(AgentRunErrorCode.UnexpectedError);
        actions.Should().ContainSingle().Which.State.Should().Be(AgentActionState.Discarded);
    }
}
