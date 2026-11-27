using HealthyFac.Insights.Application.Chats;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Insights.Infrastructure.Chats.Persistence;
using HealthyFac.Insights.IntegrationTests.Chats.Data;
using HealthyFac.Insights.IntegrationTests.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Testing.Features;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.IntegrationTests.Chats;

[Collection(InsightsCollection.Name)]
[Feature(TestFeatures.Chat)]
public sealed class ChatConstraintTests(InsightsFixture fixture)
{
    private const string CheckViolation = "violates check constraint";

    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task Save_SecondMessageWithTheSameSequenceInAChat_FailsOnSequenceIndex()
    {
        var chat = ChatConstraintTestData.CreateChatWithTurn();
        await _fixture.SaveAsync(chat);
        await _fixture.ResetNextSequenceAsync(chat.Id);

        var act = () => _fixture.AddTurnAsync(chat.Id);

        await act.Should()
            .ThrowAsync<DbUpdateException>()
            .Where(exception => exception.InnerException!.Message.Contains(ChatIndexNames.MessageSequence));
    }

    [Fact]
    public async Task Save_ASecondTurnInTheNormalWay_Succeeds()
    {
        var chat = ChatConstraintTestData.CreateChatWithTurn();
        await _fixture.SaveAsync(chat);

        var act = () => _fixture.AddTurnAsync(chat.Id);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Save_UserMessageTextAboveTheLimit_FailsOnTheCheckConstraint()
    {
        var chat = ChatConstraintTestData.CreateChat();

        chat.AddTurn(
            new string(c: 'a', ChatLimits.TextMaxLength + 1),
            [],
            ChatConstraintTestData.Now);

        var act = () => _fixture.SaveAsync(chat);

        await act.Should()
            .ThrowAsync<DbUpdateException>()
            .Where(exception => exception.InnerException!.Message.Contains(CheckViolation));
    }

    [Fact]
    public async Task Save_LinkWithABlankLabel_FailsOnTheCheckConstraint()
    {
        var chat = ChatConstraintTestData.CreateChat();

        chat.AddTurn(
            ChatConstraintTestData.Text,
            [new ChatMessageLink(ChatLinkKind.Machine, Guid.CreateVersion7(), label: "   ", position: 0)],
            ChatConstraintTestData.Now);

        var act = () => _fixture.SaveAsync(chat);

        await act.Should()
            .ThrowAsync<DbUpdateException>()
            .Where(exception => exception.InnerException!.Message.Contains(CheckViolation));
    }

    [Fact]
    public async Task Save_ChatWithAnEmojiAtTheTitleLimit_Succeeds()
    {
        var chat = ChatConstraintTestData.CreateChat();
        chat.AddTurn(ChatConstraintTestData.EmojiAtTitleLimit, [], ChatConstraintTestData.Now);

        var act = () => _fixture.SaveAsync(chat);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Save_UserMessageTextAtTheLimit_Succeeds()
    {
        var chat = ChatConstraintTestData.CreateChat();
        chat.AddTurn(new string(c: 'a', ChatLimits.TextMaxLength), [], ChatConstraintTestData.Now);

        var act = () => _fixture.SaveAsync(chat);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Update_ChatStateToAnUnknownValue_FailsOnTheEnumerationCheck()
    {
        var chat = ChatConstraintTestData.CreateChatWithTurn();
        await _fixture.SaveAsync(chat);

        var act = () => _fixture.SetChatStateAsync(chat.Id, state: "oops");

        await act.Should()
            .ThrowAsync<Exception>()
            .Where(exception => exception.ToString().Contains(CheckViolation));
    }
}
