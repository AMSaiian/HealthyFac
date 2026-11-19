using System.Text.RegularExpressions;
using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Agents.Tools.Reads;
using HealthyFac.Insights.Application.Agents.Tools.Writes;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Insights.Infrastructure.Ai;
using HealthyFac.Insights.IntegrationTests.Agents.Data;
using HealthyFac.Insights.IntegrationTests.Cases;
using HealthyFac.Insights.IntegrationTests.Cases.Data;
using HealthyFac.Insights.Messaging.WorkOrders;
using HealthyFac.Testing.Tracing;
using Microsoft.Extensions.AI;

namespace HealthyFac.Insights.IntegrationTests.Agents;

[Collection(SignalCaseCollection.Name)]
public sealed class PlannerRunTests(SignalCaseFixture fixture)
{
    private const int InputTokens = 100;
    private const int OutputTokens = 20;
    private const int ModelCallCount = 3;
    private const string ToolResultMarker = "Replace the filter";
    private const string PromptMarker = "You plan the maintenance work";

    private readonly SignalCaseFixture _fixture = fixture;

    [Fact]
    public async Task Run_WithAListAndAProposal_StoresBothCallsAndCommitsOneDraft()
    {
        await ArrangeAsync();

        var didRun = await _fixture.RunAgentWorkerOnceAsync();

        var run = (await _fixture.LoadRunsAsync()).Single(candidate => candidate.Role == AgentRole.Planner);
        var calls = await _fixture.LoadToolCallsAsync();
        var signal = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        var messages = await _fixture.ReadOutboxAsync<WorkOrderDraftProposed>();
        didRun.Should().BeTrue();
        run.State.Should().Be(AgentRunState.Completed);
        run.PromptVersion.Should().Be(expected: "planner-v4");
        run.InputTokens.Should().BeGreaterThanOrEqualTo(InputTokens * ModelCallCount);
        run.OutputTokens.Should().BeGreaterThanOrEqualTo(OutputTokens * ModelCallCount);
        calls.Select(call => call.ToolName)
            .Should()
            .Equal(ListOpenWorkOrdersToolRequest.ToolName, ProposeWorkOrderToolRequest.ToolName);
        calls.Should().OnlyContain(call => call.Outcome == ToolCallOutcome.Succeeded);
        signal.Coverage.Should().Be(SignalCoverage.Drafted);
        messages.Should().ContainSingle().Which.DraftId.Should().Be(signal.WorkOrderDraftId!.Value);
    }

    [Fact]
    public async Task Run_OfThePlanner_ShowsTheModelAndStoresOnlyEncryptedIds()
    {
        await ArrangeAsync();

        await _fixture.RunAgentWorkerOnceAsync();

        var calls = await _fixture.LoadToolCallsAsync();
        var sent = string.Join(separator: " ", _fixture.Inner.Ingest.Chat.SentTexts);
        var stored = string.Join(separator: " ", calls.Select(call => call.Arguments + call.Result));
        var rawGuid = new Regex(pattern: "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
        sent.Should().Contain(CaseTestData.Encrypt(CaseTestData.MachineId));
        rawGuid.IsMatch(sent).Should().BeFalse();
        stored.Should().Contain(CaseTestData.Encrypt(CaseTestData.FirstEpisodeId));
        rawGuid.IsMatch(stored).Should().BeFalse();
    }

    [Fact]
    public async Task Run_OfThePlanner_HasSpansWithoutThePromptAndWithoutTheToolResults()
    {
        using var capture = new SpanCapture(AiServiceExtensions.SourceName);
        await ArrangeAsync();

        await _fixture.RunAgentWorkerOnceAsync();

        var spans = capture.Spans;
        var text = string.Join(
            separator: " ",
            spans.SelectMany(span => span.TagObjects.Select(tag => $"{tag.Key}={tag.Value}")));
        spans.Should().NotBeEmpty();
        text.Should().NotContain(PromptMarker).And.NotContain(ToolResultMarker);
    }

    [Fact]
    public async Task Run_WhenTheProviderStaysUnavailable_FailsTheRunAndCommitsNothing()
    {
        await ArrangeAsync();
        _fixture.Inner.Ingest.Chat.Failure = new HttpRequestException(
            message: "unavailable", inner: null, System.Net.HttpStatusCode.ServiceUnavailable);

        var worker = _fixture.RunAgentWorkerOnceAsync();
        await DriveClockAsync(worker);
        await worker;

        var run = (await _fixture.LoadRunsAsync()).Single(candidate => candidate.Role == AgentRole.Planner);
        var messages = await _fixture.ReadOutboxAsync<WorkOrderDraftProposed>();
        run.State.Should().Be(AgentRunState.Failed);
        run.ErrorCode.Should().Be(AgentRunErrorCode.ProviderUnavailable);
        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Run_WithATransientFailureAtTheSecondModelCall_RepeatsOnlyThatCallAndStoresEachToolCallOnce()
    {
        await ArrangeAsync();
        var chat = _fixture.Inner.Ingest.Chat;
        chat.OnceFailureCall = 2;
        chat.OnceFailure = new HttpRequestException(
            message: "unavailable", inner: null, System.Net.HttpStatusCode.ServiceUnavailable);

        var worker = _fixture.RunAgentWorkerOnceAsync();
        await DriveClockAsync(worker);
        await worker;

        var run = (await _fixture.LoadRunsAsync()).Single(candidate => candidate.Role == AgentRole.Planner);
        var calls = await _fixture.LoadToolCallsAsync();
        var actions = await _fixture.LoadActionsAsync();
        run.State.Should().Be(AgentRunState.Completed);
        calls.Select(call => call.ToolName)
            .Should()
            .Equal(ListOpenWorkOrdersToolRequest.ToolName, ProposeWorkOrderToolRequest.ToolName);
        actions.Should().ContainSingle().Which.State.Should().Be(AgentActionState.Committed);
        chat.CallCount.Should().Be(expected: 4);
        chat.SentMessageCounts[index: 2].Should().Be(chat.SentMessageCounts[index: 1]);
    }

    [Fact]
    public async Task Run_WhenAReadToolMeetsADownstreamOutage_FailsTheRunWithProviderUnavailableAndCommitsNothing()
    {
        await ArrangeAsync();
        _fixture.Agents.WorkOrders.Failure = new ProviderUnavailableException(message: "Core is unavailable.");

        await _fixture.RunAgentWorkerOnceAsync();

        var run = (await _fixture.LoadRunsAsync()).Single(candidate => candidate.Role == AgentRole.Planner);
        var messages = await _fixture.ReadOutboxAsync<WorkOrderDraftProposed>();
        var actions = await _fixture.LoadActionsAsync();
        run.State.Should().Be(AgentRunState.Failed);
        run.ErrorCode.Should().Be(AgentRunErrorCode.ProviderUnavailable);
        messages.Should().BeEmpty();
        actions.Should().NotContain(action => action.State == AgentActionState.Committed);
        _fixture.Inner.Ingest.Chat.CallCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task Run_WhenAReadToolFailsWithAnArgumentError_ReturnsTheFailureToTheModelAndTheRunContinues()
    {
        await ArrangeAsync();
        _fixture.Agents.WorkOrders.Failure = new InvalidOperationException(message: "The machine is unknown.");

        await _fixture.RunAgentWorkerOnceAsync();

        var run = (await _fixture.LoadRunsAsync()).Single(candidate => candidate.Role == AgentRole.Planner);
        var calls = await _fixture.LoadToolCallsAsync();
        run.State.Should().Be(AgentRunState.Completed);
        calls.Select(call => call.Outcome).Should().Equal(ToolCallOutcome.Failed, ToolCallOutcome.Succeeded);
    }

    [Fact]
    public async Task Run_WhenTheModelClosesTheCaseAfterAFailedRead_FailsTheRunAndKeepsTheCaseOpen()
    {
        await ArrangeAsync(script: false);
        _fixture.Agents.WorkOrders.Failure = new InvalidOperationException(message: "The machine is unknown.");
        ScriptListAndClose(reason: "No work is needed because the reads failed.");

        await _fixture.RunAgentWorkerOnceAsync();

        var run = (await _fixture.LoadRunsAsync()).Single(candidate => candidate.Role == AgentRole.Planner);
        var maintenanceCase = (await _fixture.LoadCasesAsync()).Single();
        run.State.Should().Be(AgentRunState.Failed);
        run.ErrorCode.Should().Be(AgentRunErrorCode.ProviderUnavailable);
        maintenanceCase.State.Should().Be(CaseState.Open);
    }

    [Fact]
    public async Task Run_WhenTheModelClosesTheCaseAfterSuccessfulReads_ClosesTheCase()
    {
        await ArrangeAsync(script: false);
        ScriptListAndClose(reason: "No work is needed.");

        await _fixture.RunAgentWorkerOnceAsync();

        var run = (await _fixture.LoadRunsAsync()).Single(candidate => candidate.Role == AgentRole.Planner);
        var maintenanceCase = (await _fixture.LoadCasesAsync()).Single();
        run.State.Should().Be(AgentRunState.Completed);
        maintenanceCase.State.Should().Be(CaseState.Closed);
    }

    [Fact]
    public async Task
        Run_WhenTheLoopEndsWithoutAWriteAction_MakesOneDecisionTurnWithOnlyTheWriteToolsAndCommitsItsProposal()
    {
        await ArrangeAsync(script: false);
        var chat = _fixture.Inner.Ingest.Chat;
        chat.ScriptToolCall(callId: "call-1", ListOpenWorkOrdersToolRequest.ToolName, CaseTestData.EmptyJson);
        chat.ScriptText(text: "I need more time.");
        chat.ScriptToolCall(callId: "call-2", ProposeWorkOrderToolRequest.ToolName, ProposalJson());
        chat.ScriptText(text: "I proposed an inspection.");

        await _fixture.RunAgentWorkerOnceAsync();

        var run = (await _fixture.LoadRunsAsync()).Single(candidate => candidate.Role == AgentRole.Planner);
        var messages = await _fixture.ReadOutboxAsync<WorkOrderDraftProposed>();
        var decision = chat.SentOptions[index: 2]!;
        run.State.Should().Be(AgentRunState.Completed);
        run.VerdictReason.Should().Be(expected: "I proposed an inspection.");
        messages.Should().ContainSingle();
        chat.CallCount.Should().Be(expected: 4);
        decision.ToolMode.Should().BeOfType<RequiredChatToolMode>();
        decision.Tools!.Select(tool => tool.Name)
            .Should()
            .BeEquivalentTo(
                ProposeWorkOrderToolRequest.ToolName,
                LinkWorkOrderToolRequest.ToolName,
                CloseCaseToolRequest.ToolName);
        chat.SentTexts[index: 2].Should().Contain(AgentMessages.PlannerDecisionMessage);
    }

    [Fact]
    public async Task Run_WhenTheModelAlreadyProposed_MakesNoDecisionTurn()
    {
        await ArrangeAsync();

        await _fixture.RunAgentWorkerOnceAsync();

        var chat = _fixture.Inner.Ingest.Chat;
        chat.CallCount.Should().Be(expected: 3);
        chat.SentOptions.Should().OnlyContain(options => options == null || options.ToolMode == null);
    }

    [Fact]
    public async Task Run_WhenTheDecisionTurnMakesNoWriteCall_FailsWithNoDecisionStoresTheTextAndCommitsNothing()
    {
        await ArrangeAsync(script: false);
        var chat = _fixture.Inner.Ingest.Chat;
        chat.ScriptToolCall(callId: "call-1", ListOpenWorkOrdersToolRequest.ToolName, CaseTestData.EmptyJson);
        chat.ScriptText(text: "I read the work orders.");
        chat.ScriptText(text: "I cannot decide.");

        await _fixture.RunAgentWorkerOnceAsync();

        var run = (await _fixture.LoadRunsAsync()).Single(candidate => candidate.Role == AgentRole.Planner);
        var messages = await _fixture.ReadOutboxAsync<WorkOrderDraftProposed>();
        var actions = await _fixture.LoadActionsAsync();
        run.State.Should().Be(AgentRunState.Failed);
        run.ErrorCode.Should().Be(AgentRunErrorCode.NoDecision);
        run.VerdictReason.Should().Be(expected: "I cannot decide.");
        run.InputTokens.Should().BeGreaterThan(expected: 0);
        messages.Should().BeEmpty();
        actions.Should().BeEmpty();
    }

    private static string ProposalJson() =>
        $$"""
          {"episodeIds":["{{CaseTestData.Encrypt(CaseTestData.FirstEpisodeId)}}"],"title":"Check the spindle",
          "priority":"high","category":"inspection","estimateMinutes":60}
          """;

    private void ScriptListAndClose(string reason)
    {
        var chat = _fixture.Inner.Ingest.Chat;
        chat.ScriptToolCall(callId: "call-1", ListOpenWorkOrdersToolRequest.ToolName, CaseTestData.EmptyJson);
        chat.ScriptToolCall(callId: "call-2", CloseCaseToolRequest.ToolName, $$"""{"reason":"{{reason}}"}""");
        chat.ScriptText(text: "I closed the case.");
    }

    private async Task ArrangeAsync(bool script = true)
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.ClearRunsAsync();
        await _fixture.AddQueuedRunAsync(AgentRole.Planner, (await _fixture.LoadCasesAsync()).Single().Id);
        _fixture.Agents.WorkOrders.Answer.Add(
            new Application.WorkOrders.OpenWorkOrderInfo
            {
                Id = Guid.CreateVersion7(),
                Title = ToolResultMarker,
                Status = "staged",
                Priority = "high",
                Category = "repair",
            });
        var chat = _fixture.Inner.Ingest.Chat;
        chat.Usage = new UsageDetails { InputTokenCount = InputTokens, OutputTokenCount = OutputTokens };
        if (!script)
        {
            return;
        }

        chat.ScriptToolCall(callId: "call-1", ListOpenWorkOrdersToolRequest.ToolName, CaseTestData.EmptyJson);
        chat.ScriptToolCall(
            callId: "call-2",
            ProposeWorkOrderToolRequest.ToolName,
            $$"""
              {"episodeIds":["{{CaseTestData.Encrypt(CaseTestData.FirstEpisodeId)}}"],"title":"Check the spindle",
               "priority":"high",
              "category":"inspection","estimateMinutes":60}
              """);
        chat.ScriptText(text: "I proposed an inspection.");
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
