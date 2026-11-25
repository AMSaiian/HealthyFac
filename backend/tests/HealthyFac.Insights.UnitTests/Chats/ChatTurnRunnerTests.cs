using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Application.Chats.Turns;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.Domain.Chats;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Insights.UnitTests.Agents.Data;
using HealthyFac.Insights.UnitTests.Chats.Data;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Insights.UnitTests.Chats;

public sealed class ChatTurnRunnerTests
{
    [Fact]
    public async Task RunAsync_ForAnInScopeMessage_CompletesTheReplyWithTheStreamedTextAndTheTokens()
    {
        var fixture = new TurnRunnerFixture();
        fixture.Client.ScriptStream(
            ChatTurnRunnerTestData.Piece(ChatTurnRunnerTestData.FirstPiece),
            ChatTurnRunnerTestData.Piece(ChatTurnRunnerTestData.SecondPiece),
            ChatTurnRunnerTestData.Usage(input: 20, output: 5));

        await fixture.RunAsync(TestContext.Current.CancellationToken);

        fixture.Assistant.State.Should().Be(ChatMessageState.Completed);
        fixture.Assistant.Kind.Should().Be(ChatMessageKind.Answer);
        fixture.Assistant.Text.Should().Be(ChatTurnRunnerTestData.FirstPiece + ChatTurnRunnerTestData.SecondPiece);
        fixture.Assistant.AgentRunId.Should().Be(fixture.Saves.Run!.Id);
        fixture.Saves.Run.State.Should().Be(AgentRunState.Completed);
        fixture.Saves.Run.Role.Should().Be(AgentRole.Chat);
        fixture.Saves.Run.InputTokens.Should().Be(expected: 23);
        fixture.Saves.Run.OutputTokens.Should().Be(expected: 6);
        fixture.Notifier.Ends.Should().ContainSingle().Which.State.Should().Be(expected: "completed");
        fixture.Saves.SaveCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task RunAsync_AfterARefusedAndAFailedTurn_SendsOnlyTheAnsweredTurnAndTheNewMessage()
    {
        var fixture = new TurnRunnerFixture(chat =>
        {
            chat.AddTurn(text: "refused question", [], ChatTestData.Now)
                .Assistant.Complete(ChatMessageKind.Refusal, string.Empty);
            chat.AddTurn(text: "failed question", [], ChatTestData.Now)
                .Assistant.End(ChatMessageState.Failed, AgentRunErrorCode.ProviderUnavailable);
            var answered = chat.AddTurn(text: "answered question", [], ChatTestData.Now);
            answered.Assistant.Complete(ChatMessageKind.Answer, text: "the answer");
        });
        fixture.Client.ScriptStream(ChatTurnRunnerTestData.Piece(ChatTurnRunnerTestData.FirstPiece));

        await fixture.RunAsync(TestContext.Current.CancellationToken);

        var sent = fixture.Client.SentMessages.Should().ContainSingle().Subject;
        sent.Select(message => message.Text)
            .Should()
            .Equal("answered question", "the answer", ChatTestData.ShortText);
    }

    [Fact]
    public async Task RunAsync_ForAnOffTopicMessage_RefusesWithoutTextAndCallsNoChatModel()
    {
        var fixture = new TurnRunnerFixture();
        fixture.Triage.Choice = ChatTriageChoice.OffTopic;

        await fixture.RunAsync(TestContext.Current.CancellationToken);

        fixture.Assistant.Kind.Should().Be(ChatMessageKind.Refusal);
        fixture.Assistant.Text.Should().BeEmpty();
        fixture.Chat.State.Should().Be(ChatState.Open);
        fixture.Client.CallCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task RunAsync_ForAMaliciousMessage_BlocksTheChatAndCallsNoChatModel()
    {
        var fixture = new TurnRunnerFixture();
        fixture.Triage.Choice = ChatTriageChoice.Malicious;

        await fixture.RunAsync(TestContext.Current.CancellationToken);

        fixture.Assistant.Kind.Should().Be(ChatMessageKind.BlockedNotice);
        fixture.Chat.State.Should().Be(ChatState.Blocked);
        fixture.Client.CallCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task RunAsync_WhenTheTriageFails_FailsTheReplyClosedAndCallsNoChatModel()
    {
        var fixture = new TurnRunnerFixture();
        fixture.Triage.Failure = ChatTurnRunnerTestData.Unavailable();

        await fixture.RunAsync(TestContext.Current.CancellationToken);

        fixture.Assistant.State.Should().Be(ChatMessageState.Failed);
        fixture.Assistant.ErrorCode.Should().Be(AgentRunErrorCode.ProviderUnavailable);
        fixture.Saves.Run!.State.Should().Be(AgentRunState.Failed);
        fixture.Client.CallCount.Should().Be(expected: 0);
        fixture.Notifier.Ends.Should().ContainSingle().Which.State.Should().Be(expected: "failed");
    }

    [Fact]
    public async Task RunAsync_WhenTheChatModelFails_FailsTheReplyWithProviderUnavailable()
    {
        var fixture = new TurnRunnerFixture();
        fixture.Client.Failure = AgentProviderCallsTestData.Transient();

        await fixture.RunAsync(TestContext.Current.CancellationToken);

        fixture.Assistant.State.Should().Be(ChatMessageState.Failed);
        fixture.Assistant.ErrorCode.Should().Be(AgentRunErrorCode.ProviderUnavailable);
    }

    [Fact]
    public async Task RunAsync_WhenTheChatModelThrowsAnUnexpectedError_FailsTheReplyAndPushesTheEnd()
    {
        var fixture = new TurnRunnerFixture();
        fixture.Client.Failure = new InvalidOperationException();

        await fixture.RunAsync(TestContext.Current.CancellationToken);

        fixture.Assistant.State.Should().Be(ChatMessageState.Failed);
        fixture.Assistant.ErrorCode.Should().Be(AgentRunErrorCode.ProviderUnavailable);
        fixture.Notifier.Ends.Should().ContainSingle().Which.State.Should().Be(expected: "failed");
    }

    [Fact]
    public async Task RunAsync_WhenTheRequestIsCancelledAndTheModelThrowsAnIoError_EndsCancelledWithoutAnErrorLog()
    {
        var fixture = new TurnRunnerFixture();
        using var request = new CancellationTokenSource();
        fixture.Client.ScriptStream(
            ChatTurnRunnerTestData.Piece(ChatTurnRunnerTestData.FirstPiece),
            ChatTurnRunnerTestData.Piece(ChatTurnRunnerTestData.SecondPiece));
        fixture.Client.BeforeUpdate = index =>
        {
            if (index == 1)
            {
                request.Cancel();

                throw new IOException();
            }

            return Task.CompletedTask;
        };

        await fixture.RunAsync(request.Token);

        fixture.Assistant.State.Should().Be(ChatMessageState.Cancelled);
        fixture.Logger.Collector.GetSnapshot().Should().NotContain(entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public async Task RunAsync_WhenAProviderErrorFollowsTheFirstPiece_FailsWithTheFirstPieceAsText()
    {
        var fixture = new TurnRunnerFixture();
        fixture.Client.ScriptStream(
            ChatTurnRunnerTestData.Piece(ChatTurnRunnerTestData.FirstPiece),
            ChatTurnRunnerTestData.Piece(ChatTurnRunnerTestData.SecondPiece));
        fixture.Client.BeforeUpdate = index => index == 1
            ? Task.FromException(AgentProviderCallsTestData.Transient())
            : Task.CompletedTask;

        await fixture.RunAsync(TestContext.Current.CancellationToken);

        fixture.Assistant.State.Should().Be(ChatMessageState.Failed);
        fixture.Assistant.ErrorCode.Should().Be(AgentRunErrorCode.ProviderUnavailable);
        fixture.Assistant.Text.Should().Be(ChatTurnRunnerTestData.FirstPiece);
    }

    [Fact]
    public async Task RunAsync_WhenTheRequestIsCancelled_KeepsThePartialTextAndSavesWithoutTheCancelledToken()
    {
        var fixture = new TurnRunnerFixture();
        using var request = new CancellationTokenSource();
        fixture.Client.ScriptStream(
            ChatTurnRunnerTestData.Piece(ChatTurnRunnerTestData.FirstPiece),
            ChatTurnRunnerTestData.Piece(ChatTurnRunnerTestData.SecondPiece));
        fixture.Client.BeforeUpdate = index =>
        {
            if (index == 1)
            {
                request.Cancel();
            }

            return Task.CompletedTask;
        };

        await fixture.RunAsync(request.Token);

        fixture.Assistant.State.Should().Be(ChatMessageState.Cancelled);
        fixture.Assistant.Text.Should().Be(ChatTurnRunnerTestData.FirstPiece);
        fixture.Saves.Run!.State.Should().Be(AgentRunState.Interrupted);
        fixture.Saves.Run.InputTokens.Should().Be(expected: 3);
        fixture.Saves.Run.OutputTokens.Should().Be(expected: 1);
        fixture.Saves.WasTokenCancelable.Should().BeFalse();
        fixture.Notifier.Ends.Should().ContainSingle().Which.State.Should().Be(expected: "cancelled");
    }

    [Fact]
    public async Task RunAsync_WhenTheTimeLimitPasses_FailsWithTimeLimitAndKeepsThePartialText()
    {
        var fixture = new TurnRunnerFixture();
        fixture.Client.ScriptStream(
            ChatTurnRunnerTestData.Piece(ChatTurnRunnerTestData.FirstPiece),
            ChatTurnRunnerTestData.Piece(ChatTurnRunnerTestData.SecondPiece));
        fixture.Client.BeforeUpdate = index =>
        {
            if (index == 1)
            {
                fixture.Time.Advance(TimeSpan.FromSeconds(ChatTurnRunnerTestData.OverLimitSeconds));
            }

            return Task.CompletedTask;
        };

        await fixture.RunAsync(TestContext.Current.CancellationToken);

        fixture.Assistant.State.Should().Be(ChatMessageState.Failed);
        fixture.Assistant.ErrorCode.Should().Be(AgentRunErrorCode.TimeLimit);
        fixture.Assistant.Text.Should().Be(ChatTurnRunnerTestData.FirstPiece);
        fixture.Saves.Run!.State.Should().Be(AgentRunState.Failed);
    }

    [Fact]
    public async Task RunAsync_WhenTheModelHitsTheOutputLimit_FailsWithOutputLimitAndKeepsTheText()
    {
        var fixture = new TurnRunnerFixture();
        fixture.Client.ScriptStream(
            ChatTurnRunnerTestData.Piece(ChatTurnRunnerTestData.FirstPiece),
            ChatTurnRunnerTestData.Length());

        await fixture.RunAsync(TestContext.Current.CancellationToken);

        fixture.Assistant.State.Should().Be(ChatMessageState.Failed);
        fixture.Assistant.ErrorCode.Should().Be(AgentRunErrorCode.OutputLimit);
        fixture.Assistant.Text.Should().Be(ChatTurnRunnerTestData.FirstPiece);
    }

    [Fact]
    public async Task RunAsync_WhenTheModelCallsAToolOnce_StoresTheCallRowAndSetsTheActivityOfTheNextSnapshot()
    {
        var fixture = new TurnRunnerFixture();
        fixture.Client.ScriptStream(ChatTurnRunnerTestData.ToolCall(callId: "call-1"));
        fixture.Client.ScriptStream(ChatTurnRunnerTestData.Piece(ChatTurnRunnerTestData.FirstPiece));

        await fixture.RunAsync(TestContext.Current.CancellationToken);

        fixture.Assistant.State.Should().Be(ChatMessageState.Completed);
        fixture.Assistant.Text.Should().Be(ChatTurnRunnerTestData.FirstPiece);
        var row = fixture.Saves.ToolCalls.Should().ContainSingle().Subject;
        row.ToolName.Should().Be(ToolTestData.ChatEmployeesToolName);
        row.RunId.Should().Be(fixture.Saves.Run!.Id);
        row.Sequence.Should().Be(expected: 1);
        fixture.Notifier.Updates.Select(update => update.Activity).Should().Equal("people", null);
    }

    [Fact]
    public async Task RunAsync_WhenTheModelCallsMoreToolsThanTheLimit_NeverRunsTheExtraCallAndAsksOnceWithoutTools()
    {
        var fixture = new TurnRunnerFixture(maxToolCalls: 2);
        fixture.Client.ScriptStream(
            ChatTurnRunnerTestData.ToolCall(callId: "call-1"),
            ChatTurnRunnerTestData.ToolCall(callId: "call-2"),
            ChatTurnRunnerTestData.ToolCall(callId: "call-3"));
        fixture.Client.ScriptStream(ChatTurnRunnerTestData.Piece(ChatTurnRunnerTestData.SecondPiece));

        await fixture.RunAsync(TestContext.Current.CancellationToken);

        fixture.Saves.ToolCalls.Should().HaveCount(expected: 2);
        fixture.Client.CallCount.Should().Be(expected: 2);
        fixture.Client.SentOptions[index: 0]!.Tools.Should().NotBeNullOrEmpty();
        fixture.Client.SentOptions[index: 1]!.Tools.Should().BeNullOrEmpty();
        fixture.Assistant.Text.Should().Be(ChatTurnRunnerTestData.SecondPiece);
        var results = fixture.Client.SentMessages[index: 1]
            .SelectMany(message => message.Contents)
            .OfType<FunctionResultContent>()
            .ToDictionary(result => result.CallId, result => result.Result?.ToString());
        results.Keys.Should().BeEquivalentTo("call-1", "call-2", "call-3");
        results[key: "call-3"].Should().Be(ChatTurnTools.LimitReachedText);
    }

    [Fact]
    public async Task RunAsync_WhenTheNoAnswerCallIsTheCallAtTheLimit_MakesNoFinalModelCall()
    {
        var fixture = new TurnRunnerFixture(maxToolCalls: 1);
        fixture.Client.ScriptStream(
            ChatTurnRunnerTestData.ToolCall(callId: "call-1", ReportNoAnswerToolRequest.ToolName));

        await fixture.RunAsync(TestContext.Current.CancellationToken);

        fixture.Client.CallCount.Should().Be(expected: 1);
        fixture.Assistant.Kind.Should().Be(ChatMessageKind.Unanswerable);
        fixture.Notifier.Updates.Should().NotContain(update => update.Text.Length > 0);
    }

    [Fact]
    public async Task RunAsync_WhenTheModelReportsNoAnswer_CompletesUnanswerableWithoutTextAndEndsTheLoop()
    {
        var fixture = new TurnRunnerFixture();
        fixture.Client.ScriptStream(
            ChatTurnRunnerTestData.Piece(ChatTurnRunnerTestData.FirstPiece),
            ChatTurnRunnerTestData.ToolCall(callId: "call-1", ReportNoAnswerToolRequest.ToolName));

        await fixture.RunAsync(TestContext.Current.CancellationToken);

        fixture.Assistant.State.Should().Be(ChatMessageState.Completed);
        fixture.Assistant.Kind.Should().Be(ChatMessageKind.Unanswerable);
        fixture.Assistant.Text.Should().BeEmpty();
        fixture.Client.CallCount.Should().Be(expected: 1);
        fixture.Saves.Run!.State.Should().Be(AgentRunState.Completed);
    }

    [Fact]
    public async Task RunAsync_WhenTheServiceOfAToolIsDown_GivesTheModelAFixedTextAndGoesOn()
    {
        var fixture = new TurnRunnerFixture(toolFailure: ChatTurnRunnerTestData.Unavailable());
        fixture.Client.ScriptStream(ChatTurnRunnerTestData.ToolCall(callId: "call-1"));
        fixture.Client.ScriptStream(ChatTurnRunnerTestData.Piece(ChatTurnRunnerTestData.FirstPiece));

        await fixture.RunAsync(TestContext.Current.CancellationToken);

        fixture.Assistant.State.Should().Be(ChatMessageState.Completed);
        fixture.Assistant.Text.Should().Be(ChatTurnRunnerTestData.FirstPiece);
        fixture.Client.CallCount.Should().Be(expected: 2);
        var result = fixture.Client.SentMessages[index: 1]
            .SelectMany(message => message.Contents)
            .OfType<FunctionResultContent>()
            .Single();
        result.Result!.ToString().Should().Contain(ChatToolErrors.ServiceUnavailable);
        fixture.Saves.ToolCalls.Should().ContainSingle().Which.Outcome.Should().Be(ToolCallOutcome.Failed);
    }
}
