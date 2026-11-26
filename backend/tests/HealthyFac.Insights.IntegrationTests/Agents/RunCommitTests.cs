using HealthyFac.Insights.Application.Agents.Tools.Writes;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.IntegrationTests.Agents.Data;
using HealthyFac.Insights.IntegrationTests.Cases;
using HealthyFac.Insights.IntegrationTests.Cases.Data;
using HealthyFac.Insights.Messaging.WorkOrders;
using HealthyFac.Telemetry.Messaging.Signals;

namespace HealthyFac.Insights.IntegrationTests.Agents;

[Collection(SignalCaseCollection.Name)]
public sealed class RunCommitTests(SignalCaseFixture fixture)
{
    private readonly SignalCaseFixture _fixture = fixture;

    [Fact]
    public async Task Run_WhenASignalChangesBeforeTheCommit_FailsWithCaseChangedAndQueuesTriage()
    {
        await ArrangeAsync(
            CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 2, SignalEpisodeStateValue.Open));

        await _fixture.RunAgentWorkerOnceAsync();

        var run = (await _fixture.LoadRunsAsync()).Single(candidate => candidate.Role == AgentRole.Planner);
        var queued = (await _fixture.LoadRunsAsync()).Where(candidate => candidate.State == AgentRunState.Queued);
        var actions = await _fixture.LoadActionsAsync();
        var messages = await _fixture.ReadOutboxAsync<WorkOrderDraftProposed>();
        run.State.Should().Be(AgentRunState.Failed);
        run.ErrorCode.Should().Be(AgentRunErrorCode.CaseChanged);
        actions.Should().OnlyContain(action => action.State == AgentActionState.Discarded);
        messages.Should().BeEmpty();
        queued.Should().ContainSingle().Which.Role.Should().Be(AgentRole.Triage);
    }

    [Fact]
    public async Task Run_WhenTheLastUncoveredSignalResolvesBeforeTheCommit_FailsWithCaseChangedAndQueuesNoRun()
    {
        await ArrangeAsync(
            CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 2, SignalEpisodeStateValue.Resolved));

        await _fixture.RunAgentWorkerOnceAsync();

        var runs = await _fixture.LoadRunsAsync();

        runs.Single(candidate => candidate.Role == AgentRole.Planner)
            .ErrorCode.Should()
            .Be(AgentRunErrorCode.CaseChanged);

        runs.Should().NotContain(candidate => candidate.State == AgentRunState.Queued);
    }

    [Fact]
    public async Task Store_WithAnUnknownErrorCode_IsRejectedByTheDatabase()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        var runId = await _fixture.AddRunningRunAsync((await _fixture.LoadCasesAsync()).Single().Id);

        var isUnknownStored = await _fixture.TryStoreRawRunErrorCodeAsync(runId, errorCode: "unknownCode");
        var isKnownStored = await _fixture.TryStoreRawRunErrorCodeAsync(runId, errorCode: "caseChanged");

        isUnknownStored.Should().BeFalse();
        isKnownStored.Should().BeTrue();
    }

    [Fact]
    public async Task Store_WithAnUnknownVerdict_IsRejectedByTheDatabase()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        var runId = await _fixture.AddRunningRunAsync((await _fixture.LoadCasesAsync()).Single().Id);

        var isUnknownStored = await _fixture.TryStoreRawRunVerdictAsync(runId, verdict: "maybe");
        var isKnownStored = await _fixture.TryStoreRawRunVerdictAsync(runId, verdict: "unsafe");

        isUnknownStored.Should().BeFalse();
        isKnownStored.Should().BeTrue();
    }

    private async Task ArrangeAsync(SignalEpisodeChanged change)
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.ClearRunsAsync();
        await _fixture.AddQueuedRunAsync(AgentRole.Planner, (await _fixture.LoadCasesAsync()).Single().Id);
        var chat = _fixture.Inner.Ingest.Chat;
        var isChanged = false;

        chat.Before = async () =>
        {
            if (isChanged || chat.CallCount < 2)
            {
                return;
            }

            isChanged = true;
            await _fixture.HandleSignalAsync(change);
            await _fixture.DeleteQueuedRunsAsync();
        };

        chat.ScriptToolCall(
            callId: "call-1",
            ProposeWorkOrderToolRequest.ToolName,
            $$"""
              {"episodeIds":["{{CaseTestData.Encrypt(CaseTestData.FirstEpisodeId)}}"],"title":"Check the spindle",
               "priority":"high",
              "category":"inspection","estimateMinutes":60}
              """);

        chat.ScriptText(text: "Done.");
    }
}
