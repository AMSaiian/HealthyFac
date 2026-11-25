using HealthyFac.Insights.Application.Chats.Turns;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Chats;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Insights.UnitTests.Agents.Data;
using HealthyFac.Insights.UnitTests.Chats.Data;

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
}
