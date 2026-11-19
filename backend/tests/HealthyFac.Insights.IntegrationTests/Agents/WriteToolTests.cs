using HealthyFac.Insights.Application.Agents.Tools.Writes;
using HealthyFac.Insights.Application.WorkOrders;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Insights.IntegrationTests.Agents.Data;
using HealthyFac.Insights.IntegrationTests.Cases;
using HealthyFac.Insights.IntegrationTests.Cases.Data;
using HealthyFac.Insights.Messaging.WorkOrders;
using HealthyFac.Telemetry.Messaging.Signals;

namespace HealthyFac.Insights.IntegrationTests.Agents;

[Collection(SignalCaseCollection.Name)]
public sealed class WriteToolTests(SignalCaseFixture fixture)
{
    private const string Reason = "No work is needed.";
    private const string RepeatedCallId = "call-repeat";

    private static readonly Guid OpenWorkOrderId = Guid.Parse(input: "44444444-4444-4444-4444-444444444444");
    private static readonly Guid OtherWorkOrderId = Guid.Parse(input: "55555555-5555-5555-5555-555555555555");

    private readonly SignalCaseFixture _fixture = fixture;

    [Fact]
    public async Task Propose_ThroughTheTool_BuffersTheActionAndChangesNothingUntilTheRunEnds()
    {
        var runId = await ArrangeAsync();

        await _fixture.CallToolAsync(
            ProposeWorkOrderToolRequest.ToolName,
            Propose(CaseTestData.FirstEpisodeId),
            existingRunId: runId);

        var actions = await _fixture.LoadActionsAsync();
        var signal = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        var messages = await _fixture.ReadOutboxAsync<WorkOrderDraftProposed>();
        actions.Should().ContainSingle().Which.State.Should().Be(AgentActionState.Buffered);
        signal.Coverage.Should().Be(SignalCoverage.New);
        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Commit_WithAProposalALinkAndAClose_AppliesEveryActionAndSendsTheProposal()
    {
        var runId = await ArrangeAsync();
        _fixture.Agents.WorkOrders.Answer.Add(OpenWorkOrder(OpenWorkOrderId));
        await _fixture.CallToolAsync(
            ProposeWorkOrderToolRequest.ToolName,
            Propose(CaseTestData.FirstEpisodeId),
            existingRunId: runId);
        await _fixture.CallToolAsync(
            LinkWorkOrderToolRequest.ToolName,
            LinkArguments(OpenWorkOrderId, CaseTestData.SecondEpisodeId),
            existingRunId: runId);
        await _fixture.CallToolAsync(
            CloseCaseToolRequest.ToolName,
            $$"""{"reason":"{{Reason}}"}""",
            existingRunId: runId);

        await _fixture.CommitRunAsync(runId);

        var actions = await _fixture.LoadActionsAsync();
        var drafted = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        var linked = await _fixture.LoadSignalAsync(CaseTestData.SecondEpisodeId);
        var maintenanceCase = (await _fixture.LoadCasesAsync()).Single();
        var run = await _fixture.LoadRunAsync(runId);
        var messages = await _fixture.ReadOutboxAsync<WorkOrderDraftProposed>();
        var proposal = actions.Single(action => action.Kind == AgentActionKind.ProposeWorkOrder);
        actions.Should().OnlyContain(action => action.State == AgentActionState.Committed);
        drafted.Coverage.Should().Be(SignalCoverage.Drafted);
        drafted.WorkOrderDraftId.Should().Be(proposal.Id);
        linked.Coverage.Should().Be(SignalCoverage.Linked);
        linked.WorkOrderId.Should().Be(OpenWorkOrderId);
        maintenanceCase.State.Should().Be(CaseState.Closed);
        run.State.Should().Be(AgentRunState.Completed);
        var message = messages.Should().ContainSingle().Subject;
        message.DraftId.Should().Be(proposal.Id);
        message.CaseId.Should().Be(maintenanceCase.Id);
        message.RunId.Should().Be(runId);
    }

    [Fact]
    public async Task Fail_WithBufferedActions_DiscardsThemAndSendsNothing()
    {
        var runId = await ArrangeAsync();
        await _fixture.CallToolAsync(
            ProposeWorkOrderToolRequest.ToolName,
            Propose(CaseTestData.FirstEpisodeId),
            existingRunId: runId);

        await _fixture.FailRunAsync(runId);

        var actions = await _fixture.LoadActionsAsync();
        var signal = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        var run = await _fixture.LoadRunAsync(runId);
        var messages = await _fixture.ReadOutboxAsync<WorkOrderDraftProposed>();
        actions.Should().ContainSingle().Which.State.Should().Be(AgentActionState.Discarded);
        signal.Coverage.Should().Be(SignalCoverage.New);
        run.State.Should().Be(AgentRunState.Failed);
        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Commit_Twice_StoresOneActionAndOneMessage()
    {
        var runId = await ArrangeAsync();
        await _fixture.CallToolAsync(
            ProposeWorkOrderToolRequest.ToolName,
            Propose(CaseTestData.FirstEpisodeId),
            existingRunId: runId);
        await _fixture.CommitRunAsync(runId);

        await _fixture.CommitRunAsync(runId);

        var actions = await _fixture.LoadActionsAsync();
        var messages = await _fixture.ReadOutboxAsync<WorkOrderDraftProposed>();
        actions.Should().ContainSingle();
        messages.Should().ContainSingle();
    }

    [Fact]
    public async Task Propose_WithTheSameCallIdTwice_BuffersOneAction()
    {
        var runId = await ArrangeAsync();

        await _fixture.CallToolAsync(
            ProposeWorkOrderToolRequest.ToolName,
            Propose(CaseTestData.FirstEpisodeId),
            existingRunId: runId,
            callId: RepeatedCallId);
        await _fixture.CallToolAsync(
            ProposeWorkOrderToolRequest.ToolName,
            Propose(CaseTestData.FirstEpisodeId),
            existingRunId: runId,
            callId: RepeatedCallId);

        var actions = await _fixture.LoadActionsAsync();
        var calls = await _fixture.LoadToolCallsAsync();
        actions.Should().ContainSingle();
        calls.Should().OnlyContain(call => call.Outcome == ToolCallOutcome.Succeeded);
    }

    [Fact]
    public async Task Propose_ForTwoEpisodes_BuffersTwoActionsWithTheirOwnEpisodes()
    {
        var runId = await ArrangeAsync();

        await _fixture.CallToolAsync(
            ProposeWorkOrderToolRequest.ToolName,
            Propose(CaseTestData.FirstEpisodeId),
            existingRunId: runId);
        await _fixture.CallToolAsync(
            ProposeWorkOrderToolRequest.ToolName,
            Propose(CaseTestData.SecondEpisodeId),
            existingRunId: runId);
        await _fixture.CommitRunAsync(runId);

        var first = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        var second = await _fixture.LoadSignalAsync(CaseTestData.SecondEpisodeId);
        var messages = await _fixture.ReadOutboxAsync<WorkOrderDraftProposed>();
        messages.Should().HaveCount(expected: 2);
        first.WorkOrderDraftId.Should().NotBe(second.WorkOrderDraftId!.Value);
        messages.Select(message => message.DraftId)
            .Should()
            .BeEquivalentTo([first.WorkOrderDraftId!.Value, second.WorkOrderDraftId!.Value]);
    }

    [Fact]
    public async Task Propose_ForAnEpisodeOfAnEarlierActionOfTheRun_IsRejectedAndBuffersNothingMore()
    {
        var runId = await ArrangeAsync();
        await _fixture.CallToolAsync(
            ProposeWorkOrderToolRequest.ToolName,
            Propose(CaseTestData.FirstEpisodeId),
            existingRunId: runId);

        await _fixture.CallToolAsync(
            ProposeWorkOrderToolRequest.ToolName,
            Propose(CaseTestData.FirstEpisodeId),
            existingRunId: runId);

        var actions = await _fixture.LoadActionsAsync();
        var calls = await _fixture.LoadToolCallsAsync();
        actions.Should().ContainSingle();
        calls.Last().Outcome.Should().Be(ToolCallOutcome.Rejected);
    }

    [Fact]
    public async Task Propose_ForAResolvedEpisode_IsRejected()
    {
        var runId = await ArrangeAsync();
        await _fixture.HandleSignalAsync(
            CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 2, SignalEpisodeStateValue.Resolved));

        await _fixture.CallToolAsync(
            ProposeWorkOrderToolRequest.ToolName,
            Propose(CaseTestData.FirstEpisodeId),
            existingRunId: runId);

        var actions = await _fixture.LoadActionsAsync();
        var calls = await _fixture.LoadToolCallsAsync();
        actions.Should().BeEmpty();
        calls.Single().Outcome.Should().Be(ToolCallOutcome.Rejected);
    }

    [Fact]
    public async Task Propose_ForAnEpisodeOfAnotherCase_IsRejected()
    {
        var runId = await ArrangeAsync();
        await _fixture.HandleSignalAsync(
            CaseTestData.Message(CaseTestData.ThirdEpisodeId, sequence: 1, machineId: CaseTestData.OtherMachineId));

        await _fixture.CallToolAsync(
            ProposeWorkOrderToolRequest.ToolName,
            Propose(CaseTestData.ThirdEpisodeId),
            existingRunId: runId);

        var actions = await _fixture.LoadActionsAsync();
        var calls = await _fixture.LoadToolCallsAsync();
        actions.Should().BeEmpty();
        calls.Single().Outcome.Should().Be(ToolCallOutcome.Rejected);
    }

    [Fact]
    public async Task Link_ForAWorkOrderOfAnotherMachine_IsRejectedAndBuffersNothing()
    {
        var runId = await ArrangeAsync();
        _fixture.Agents.WorkOrders.Answer.Add(OpenWorkOrder(OpenWorkOrderId));

        await _fixture.CallToolAsync(
            LinkWorkOrderToolRequest.ToolName,
            LinkArguments(OtherWorkOrderId, CaseTestData.FirstEpisodeId),
            existingRunId: runId);

        var actions = await _fixture.LoadActionsAsync();
        var calls = await _fixture.LoadToolCallsAsync();
        actions.Should().BeEmpty();
        calls.Single().Outcome.Should().Be(ToolCallOutcome.Rejected);
    }

    [Fact]
    public async Task Link_ForAnOpenWorkOrder_SetsTheSignalsLinkedAndSendsNoProposal()
    {
        var runId = await ArrangeAsync();
        _fixture.Agents.WorkOrders.Answer.Add(OpenWorkOrder(OpenWorkOrderId));
        await _fixture.CallToolAsync(
            LinkWorkOrderToolRequest.ToolName,
            LinkArguments(OpenWorkOrderId, CaseTestData.FirstEpisodeId),
            existingRunId: runId);

        await _fixture.CommitRunAsync(runId);

        var signal = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        var messages = await _fixture.ReadOutboxAsync<WorkOrderDraftProposed>();
        signal.Coverage.Should().Be(SignalCoverage.Linked);
        signal.WorkOrderId.Should().Be(OpenWorkOrderId);
        messages.Should().BeEmpty();
    }

    private static string LinkArguments(Guid workOrderId, Guid episodeId) =>
        $$"""
          {"workOrderId":"{{CaseTestData.Encrypt(workOrderId)}}",
          "episodeIds":["{{CaseTestData.Encrypt(episodeId)}}"]}
          """;

    private static string Propose(Guid episodeId) =>
        $$"""
          {"episodeIds":["{{CaseTestData.Encrypt(episodeId)}}"],"title":"Replace the filter","priority":"high",
           "category":"repair",
          "estimateMinutes":60}
          """;

    private static OpenWorkOrderInfo OpenWorkOrder(Guid id) =>
        new()
        {
            Id = id,
            Title = "Replace the filter",
            Status = "staged",
            Priority = "high",
            Category = "repair",
        };

    private async Task<Guid> ArrangeAsync()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.SecondEpisodeId, sequence: 1));
        await _fixture.ClearRunsAsync();

        return await _fixture.AddRunningRunAsync((await _fixture.LoadCasesAsync()).Single().Id);
    }
}
