using HealthyFac.Insights.Application.Chats;
using HealthyFac.Insights.Application.Chats.Links;
using HealthyFac.Insights.Application.Chats.Turns;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Chats;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Insights.UnitTests.Chats.Data;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace HealthyFac.Insights.UnitTests.Chats;

[Feature(TestFeatures.Chat)]
public sealed class ChatHistoryBuilderTests
{
    private const string NewText = "new question";

    private readonly ScriptedChatSummaryModel _summaryModel = new();
    private readonly FakeLogger<ChatHistoryBuilder> _logger = new();
    private readonly ChatHistoryBuilder _builder;

    public ChatHistoryBuilderTests()
    {
        _builder = new ChatHistoryBuilder(_summaryModel, _logger);
    }

    [Fact]
    public async Task BuildAsync_WithMoreMessagesThanTheThreshold_SummarizesTheOlderOnesAndKeepsTheLastTen()
    {
        var chat = CreateChat(turnCount: 16);

        var (messages, _) = await BuildAsync(chat);

        _summaryModel.CallCount.Should().Be(expected: 1);
        _summaryModel.SentMessages.Should().HaveCount(32 - ChatLimits.KeptMessageCount);
        chat.Summary.Should().Be(ScriptedChatSummaryModel.Summary);
        chat.SummaryThroughSequence.Should().Be(32 - ChatLimits.KeptMessageCount);
        messages.Should().HaveCount(1 + ChatLimits.KeptMessageCount + 1);
        messages[0].Text.Should().Contain(ScriptedChatSummaryModel.Summary);
        messages[^1].Text.Should().Be(NewText);
    }

    [Fact]
    public async Task BuildAsync_WithAFreshSummary_SendsTheSummaryAndOnlyTheMessagesAfterIt()
    {
        var chat = CreateChat(turnCount: 3);
        chat.SetSummary(text: "earlier facts", throughSequence: 2);

        var (messages, _) = await BuildAsync(chat);

        _summaryModel.CallCount.Should().Be(expected: 0);

        messages.Select(message => message.Text)
            .Should()
            .Equal(
                messages[0].Text,
                "question 2",
                "answer 2",
                "question 3",
                "answer 3",
                NewText);

        messages[0].Text.Should().Contain(expected: "earlier facts");
    }

    [Fact]
    public async Task BuildAsync_WithADeletedMessage_NeverSendsItsTurn()
    {
        var chat = CreateChat(turnCount: 3);
        var deleted = chat.Messages.Single(message => message.Text == "question 2");
        SoftDelete(chat, deleted.Id);

        var (messages, _) = await BuildAsync(chat);

        messages.Select(message => message.Text)
            .Should()
            .Equal("question 1", "answer 1", "question 3", "answer 3", NewText);
    }

    [Fact]
    public async Task BuildAsync_WithAStaleSummary_RebuildsItFromTheRemainingMessagesWithoutThePreviousSummary()
    {
        var chat = CreateChat(turnCount: 8);
        chat.SetSummary(text: "old summary", throughSequence: 6);
        var deleted = chat.Messages.Single(message => message.Text == "question 2");
        SoftDelete(chat, deleted.Id);

        await BuildAsync(chat);

        chat.IsSummaryStale.Should().BeFalse();
        _summaryModel.PreviousSummary.Should().BeNull();
        _summaryModel.SentMessages.Select(message => message.Text).Should().NotContain(unexpected: "question 2");
        _summaryModel.SentMessages.Should().HaveCount(14 - ChatLimits.KeptMessageCount);
    }

    [Fact]
    public async Task BuildAsync_WhenTheSummaryFails_KeepsTheLastTenMessagesAndLogsOneWarning()
    {
        _summaryModel.Failure = ChatTurnRunnerTestData.Unavailable();
        var chat = CreateChat(turnCount: 16);

        var (messages, _) = await BuildAsync(chat);

        messages.Should().HaveCount(ChatLimits.KeptMessageCount + 1);
        chat.Summary.Should().BeNull();
        _logger.Collector.GetSnapshot().Should().ContainSingle().Which.Level.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public async Task BuildAsync_WithARefusedAndAFailedTurn_LeavesBothTurnsOut()
    {
        var chat = new Chat(ChatTestData.OwnerId, ChatTestData.CompanyId, ChatTestData.Now);

        chat.AddTurn(text: "refused question", [], ChatTestData.Now)
            .Assistant.Complete(ChatMessageKind.Refusal, text: "");

        chat.AddTurn(text: "failed question", [], ChatTestData.Now)
            .Assistant.End(ChatMessageState.Failed, AgentRunErrorCode.ProviderUnavailable);

        chat.AddTurn(text: "answered question", [], ChatTestData.Now)
            .Assistant.Complete(ChatMessageKind.Answer, text: "ok");

        var (messages, _) = await BuildAsync(chat);

        messages.Select(message => message.Text).Should().Equal("answered question", "ok", NewText);
    }

    [Fact]
    public async Task BuildAsync_WithLinks_AppendsTheirContextBlocksToTheNewMessage()
    {
        var chat = new Chat(ChatTestData.OwnerId, ChatTestData.CompanyId, ChatTestData.Now);

        var link = new ResolvedLink
        {
            Kind = ChatLinkKind.Machine,
            EntityId = Guid.CreateVersion7(),
            Position = 0,
            Label = "Lathe 7",
            ContextBlock = "<tool_result>machine data</tool_result>",
        };

        var (messages, _) = await BuildAsync(chat, [link]);

        messages.Should().ContainSingle().Which.Text.Should().Be(NewText + Environment.NewLine + link.ContextBlock);
    }

    private static void SoftDelete(Chat chat, Guid messageId)
    {
        foreach (var message in chat.DeleteUserMessage(messageId, ChatTestData.Now))
        {
            typeof(ChatMessage).GetProperty(nameof(ChatMessage.DeletedAt))!.SetValue(message, ChatTestData.Now);
        }
    }

    private static Chat CreateChat(int turnCount)
    {
        var chat = new Chat(ChatTestData.OwnerId, ChatTestData.CompanyId, ChatTestData.Now);

        for (var turn = 1; turn <= turnCount; turn++)
        {
            chat.AddTurn($"question {turn}", [], ChatTestData.Now)
                .Assistant.Complete(ChatMessageKind.Answer, $"answer {turn}");
        }

        return chat;
    }

    private async Task<(List<ChatPromptMessage> Messages, ChatMessage User)> BuildAsync(
        Chat chat,
        IReadOnlyList<ResolvedLink>? links = null
    )
    {
        var (user, assistant) = chat.AddTurn(NewText, [], ChatTestData.Now);

        var messages = await _builder.BuildAsync(
            chat,
            user,
            assistant,
            links ?? [],
            TestContext.Current.CancellationToken);

        return (messages, user);
    }
}
