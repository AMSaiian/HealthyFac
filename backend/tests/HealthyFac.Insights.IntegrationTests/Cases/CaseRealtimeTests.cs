using HealthyFac.Insights.Application.Agents.Tools.Reads;
using HealthyFac.Insights.Application.Agents.Tools.Writes;
using HealthyFac.Insights.Application.Cases;
using HealthyFac.Insights.Application.Realtime;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Insights.IntegrationTests.Agents.Data;
using HealthyFac.Insights.IntegrationTests.Cases.Data;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Telemetry.Messaging.Signals;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Cases;

[Collection(SignalCaseCollection.Name)]
public sealed class CaseRealtimeTests(SignalCaseFixture fixture)
{
    private const string CloseReason = "The machine was replaced.";

    private readonly SignalCaseFixture _fixture = fixture;

    [Fact]
    public async Task Receive_WithAnEpisodeThatOpensACase_RecordsCaseOpenedAndTheCasesHint()
    {
        var outcome = await _fixture.ReceiveSignalThroughInboxAsync(
            CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1, severity: SignalSeverityValue.Alarm));

        var caseId = (await _fixture.LoadCasesAsync()).Single().Id;
        var pushes = PushesOf();
        outcome.IsProcessed.Should().BeTrue(outcome.Error);

        var opened = pushes.Should()
            .ContainSingle(push => push.Group
                == RealtimeGroups.ForKind(
                    CaseTestData.CompanyId,
                    RealtimeNotificationKind.MaintenanceCaseOpened))
            .Subject;

        opened.Notification!.Data.Should()
            .BeEquivalentTo(new MaintenanceCaseOpenedData { CaseId = caseId, Severity = SignalSeverity.Alarm });

        pushes.Should()
            .ContainSingle(push => push.Hint != null && push.Hint.Topic == RealtimeTopic.Cases)
            .Which.Hint!.CaseId.Should()
            .Be(caseId);
    }

    [Fact]
    public async Task Receive_WithAnEpisodeThatJoinsTheOpenCase_RecordsNoCaseOpened()
    {
        await _fixture.ReceiveSignalThroughInboxAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        _fixture.Inner.RealtimeQueue.Clear();

        var outcome = await _fixture.ReceiveSignalThroughInboxAsync(
            CaseTestData.Message(CaseTestData.SecondEpisodeId, sequence: 1));

        outcome.IsProcessed.Should().BeTrue(outcome.Error);
        PushesOf().Should().BeEmpty();
    }

    [Fact]
    public async Task Receive_WithTheLastEpisodeResolved_RecordsTheCasesHintWithTheCase()
    {
        await _fixture.ReceiveSignalThroughInboxAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        var caseId = (await _fixture.LoadCasesAsync()).Single().Id;
        _fixture.Inner.RealtimeQueue.Clear();

        var outcome = await _fixture.ReceiveSignalThroughInboxAsync(
            CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 2, SignalEpisodeStateValue.Resolved));

        outcome.IsProcessed.Should().BeTrue(outcome.Error);
        var push = PushesOf().Should().ContainSingle().Subject;
        push.Hint!.Topic.Should().Be(RealtimeTopic.Cases);
        push.Hint.CaseId.Should().Be(caseId);
    }

    [Fact]
    public async Task CloseCaseByUser_WithAnOpenCase_RecordsTheCasesHintWithTheCase()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        var caseId = (await _fixture.LoadCasesAsync()).Single().Id;
        _fixture.Inner.RealtimeQueue.Clear();

        await CloseAsync(caseId);

        var push = PushesOf().Should().ContainSingle().Subject;
        push.Hint!.Topic.Should().Be(RealtimeTopic.Cases);
        push.Hint.CaseId.Should().Be(caseId);
    }

    [Fact]
    public async Task Run_WhenThePlannerCompletes_RecordsTheCasesHintWithTheCase()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.ClearRunsAsync();
        var caseId = (await _fixture.LoadCasesAsync()).Single().Id;
        await _fixture.AddQueuedRunAsync(AgentRole.Planner, caseId);
        var chat = _fixture.Inner.Ingest.Chat;
        chat.ScriptToolCall(callId: "call-1", ListOpenWorkOrdersToolRequest.ToolName, CaseTestData.EmptyJson);

        chat.ScriptToolCall(
            callId: "call-2",
            ProposeWorkOrderToolRequest.ToolName,
            $$"""
              {"episodeIds":["{{CaseTestData.Encrypt(CaseTestData.FirstEpisodeId)}}"],"title":"Check the spindle",
               "priority":"high","category":"inspection","estimateMinutes":60}
              """);

        chat.ScriptText(text: "I proposed an inspection.");
        _fixture.Inner.RealtimeQueue.Clear();

        await _fixture.RunAgentWorkerOnceAsync();

        var run = (await _fixture.LoadRunsAsync()).Single(candidate => candidate.Role == AgentRole.Planner);
        run.State.Should().Be(AgentRunState.Completed);
        var push = PushesOf().Should().ContainSingle().Subject;
        push.Hint!.Topic.Should().Be(RealtimeTopic.Cases);
        push.Hint.CaseId.Should().Be(caseId);
    }

    private List<RealtimePush> PushesOf() =>
        [.. _fixture.Inner.RealtimeQueue.Items.Select(item => item.Push)];

    private async Task CloseAsync(Guid caseId)
    {
        using var scope = _fixture.Inner.CreateActorScope(
            new ActorContext
            {
                Kind = ActorKind.User,
                UserId = Guid.CreateVersion7(),
                CompanyId = CaseTestData.CompanyId,
            });

        await scope.ServiceProvider.GetRequiredService<IRequestHandler<CloseCaseByUserRequest>>()
            .HandleAsync(
                new CloseCaseByUserRequest { CompanyId = CaseTestData.CompanyId, Id = caseId, Reason = CloseReason });
    }
}
