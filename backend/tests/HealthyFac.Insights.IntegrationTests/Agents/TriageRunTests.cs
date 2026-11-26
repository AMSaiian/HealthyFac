using System.Net;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Insights.IntegrationTests.Agents.Data;
using HealthyFac.Insights.IntegrationTests.Cases;
using HealthyFac.Insights.IntegrationTests.Cases.Data;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.AI;

namespace HealthyFac.Insights.IntegrationTests.Agents;

[Collection(SignalCaseCollection.Name)]
[Feature(TestFeatures.Cases)]
public sealed class TriageRunTests(SignalCaseFixture fixture)
{
    private readonly SignalCaseFixture _fixture = fixture;

    [Fact]
    public async Task Run_WithTheVerdictDismiss_DismissesTheSignalsAndClosesTheCase()
    {
        await ArrangeAsync(TriageTestData.Answer(verdict: "dismiss"));

        await _fixture.RunAgentWorkerOnceAsync();

        var signal = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        var maintenanceCase = (await _fixture.LoadCasesAsync()).Single();
        var storedRun = (await _fixture.LoadRunsAsync()).Single();
        storedRun.Verdict.Should().Be(AgentRunVerdict.Dismiss);
        signal.Coverage.Should().Be(SignalCoverage.Dismissed);
        maintenanceCase.State.Should().Be(CaseState.Closed);
        maintenanceCase.CloseReason.Should().Be(expected: "triage");
    }

    [Fact]
    public async Task Run_WithTheVerdictDismissAndAnOpenDraftedSignal_KeepsTheCaseOpen()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.SecondEpisodeId, sequence: 1));
        await _fixture.DraftSignalAsync(CaseTestData.FirstEpisodeId);
        _fixture.Agents.TriageChat.Script(TriageTestData.Answer(verdict: "dismiss"));
        _fixture.Time.Advance(TimeSpan.FromSeconds(CaseTestData.DebounceSeconds));

        await _fixture.RunAgentWorkerOnceAsync();

        var drafted = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        var uncovered = await _fixture.LoadSignalAsync(CaseTestData.SecondEpisodeId);
        var maintenanceCase = (await _fixture.LoadCasesAsync()).Single();
        drafted.Coverage.Should().Be(SignalCoverage.Drafted);
        uncovered.Coverage.Should().Be(SignalCoverage.Dismissed);
        maintenanceCase.State.Should().Be(CaseState.Open);
    }

    [Fact]
    public async Task Run_WithTheVerdictWatch_WatchesTheSignalsAndKeepsTheCaseOpen()
    {
        await ArrangeAsync(TriageTestData.Answer(verdict: "watch"));

        await _fixture.RunAgentWorkerOnceAsync();

        var signal = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        var maintenanceCase = (await _fixture.LoadCasesAsync()).Single();
        signal.Coverage.Should().Be(SignalCoverage.Watched);
        maintenanceCase.State.Should().Be(CaseState.Open);
    }

    [Fact]
    public async Task Run_WithTheVerdictEscalate_QueuesAPlannerRunDueNow()
    {
        await ArrangeAsync(TriageTestData.Answer(verdict: "escalate"));

        await _fixture.RunAgentWorkerOnceAsync();

        var runs = await _fixture.LoadRunsAsync();
        var signal = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        var planner = runs.Should().ContainSingle(run => run.Role == AgentRole.Planner).Subject;
        runs.Single(run => run.Role == AgentRole.Triage).Verdict.Should().Be(AgentRunVerdict.Escalate);
        planner.State.Should().Be(AgentRunState.Queued);
        planner.DueAt.Should().Be(_fixture.Time.GetUtcNow().UtcDateTime);
        signal.Coverage.Should().Be(SignalCoverage.New);
    }

    [Fact]
    public async Task Run_WithAnAnswer_StoresTheRunOfTheLog()
    {
        await ArrangeAsync(TriageTestData.Answer(verdict: "watch"));

        _fixture.Agents.TriageChat.Usage = new UsageDetails
        {
            InputTokenCount = TriageTestData.InputTokens,
            OutputTokenCount = TriageTestData.OutputTokens,
        };

        await _fixture.RunAgentWorkerOnceAsync();

        var run = (await _fixture.LoadRunsAsync()).Single(candidate => candidate.Role == AgentRole.Triage);
        run.State.Should().Be(AgentRunState.Completed);
        run.Model.Should().Be(TriageTestData.TriageModel);
        run.PromptVersion.Should().Be(TriageTestData.PromptVersion);
        run.PromptHash.Should().MatchRegex(regularExpression: "^[0-9a-f]{64}$");
        run.AttemptNumber.Should().Be(expected: 1);
        run.InputTokens.Should().Be(TriageTestData.InputTokens);
        run.OutputTokens.Should().Be(TriageTestData.OutputTokens);
        run.Verdict.Should().Be(AgentRunVerdict.Watch);
        run.VerdictReason.Should().Be(TriageTestData.Reason);
    }

    [Fact]
    public async Task Run_WithTheCaseAsADataBlock_SendsTheEscapedSnapshotWithoutNames()
    {
        await ArrangeAsync(TriageTestData.Answer(verdict: "watch"));

        await _fixture.RunAgentWorkerOnceAsync();

        var sent = _fixture.Agents.TriageChat.SentTexts.Should().ContainSingle().Subject;

        sent.Should()
            .Contain(expected: "<case>")
            .And.Contain(expected: "</case>")
            .And.Contain(expected: "spindle.temperature");

        sent.Should()
            .Contain(CaseTestData.Encrypt(CaseTestData.MachineId))
            .And.NotContain(CaseTestData.MachineId.ToString());
    }

    [Fact]
    public async Task Run_WhenTheProviderStaysUnavailable_FailsAfterFourCallsAndChangesNoSignal()
    {
        await ArrangeAsync(TriageTestData.Answer(verdict: "dismiss"));

        _fixture.Agents.TriageChat.Failure = new HttpRequestException(
            message: "unavailable", inner: null, HttpStatusCode.ServiceUnavailable);

        var worker = _fixture.RunAgentWorkerOnceAsync();
        await DriveClockAsync(worker);
        await worker;

        var run = (await _fixture.LoadRunsAsync()).Single();
        var signal = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        var maintenanceCase = (await _fixture.LoadCasesAsync()).Single();
        _fixture.Agents.TriageChat.CallCount.Should().Be(TriageTestData.ProviderCallCount);
        run.State.Should().Be(AgentRunState.Failed);
        run.ErrorCode.Should().Be(AgentRunErrorCode.ProviderUnavailable);
        signal.Coverage.Should().Be(SignalCoverage.New);
        maintenanceCase.State.Should().Be(CaseState.Open);
    }

    [Fact]
    public async Task Run_WithAnAnswerThatBreaksTheSchema_FailsAtOnceWithoutRetry()
    {
        await ArrangeAsync(answer: "This is not JSON.");

        await _fixture.RunAgentWorkerOnceAsync();

        var run = (await _fixture.LoadRunsAsync()).Single();
        _fixture.Agents.TriageChat.CallCount.Should().Be(expected: 1);
        run.State.Should().Be(AgentRunState.Failed);
        run.ErrorCode.Should().Be(AgentRunErrorCode.ProviderUnavailable);
    }

    private async Task ArrangeAsync(string answer)
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        _fixture.Agents.TriageChat.Script(answer);
        _fixture.Time.Advance(TimeSpan.FromSeconds(CaseTestData.DebounceSeconds));
    }

    private async Task DriveClockAsync(Task task)
    {
        for (var step = 0; step < TriageTestData.MaxClockSteps && !task.IsCompleted; step++)
        {
            _fixture.Time.Advance(TimeSpan.FromSeconds(seconds: 1));
            await Task.Delay(millisecondsDelay: 5);
        }
    }
}
