using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Insights.IntegrationTests.Chats.Data;
using HealthyFac.Insights.IntegrationTests.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.IntegrationTests.Chats;

[Collection(InsightsCollection.Name)]
[Feature(TestFeatures.Chat)]
public sealed class ChatReadTests(InsightsFixture fixture)
{
    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task SearchChats_ForAUser_ReturnsOnlyTheChatsOfThatUserInThatCompany()
    {
        var own = await _fixture.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 1);
        await _fixture.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.OtherCompanyId, turnCount: 1);
        await _fixture.SaveChatAsync(ChatTestData.OtherUserId, ChatTestData.CompanyId, turnCount: 1);
        var deleted = await _fixture.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 1);
        await _fixture.SoftDeleteChatAsync(deleted.Id);

        var response = await _fixture.SearchChatsUseCaseAsync(
            ChatTestData.OwnerId,
            ChatTestData.CompanyId,
            limit: 10);

        response.Items.Select(item => item.Id).Should().Equal(own.Id);
        response.Items.Single().Title.Should().Be($"{ChatTestData.Question} 0");
    }

    [Fact]
    public async Task SearchChats_InPagesOfOne_ReturnsEachChatOnceWithTheLatestActivityFirst()
    {
        var hour = TimeSpan.FromHours(hours: 1);

        var first = await _fixture.SaveChatAsync(
            ChatTestData.OwnerId,
            ChatTestData.CompanyId,
            turnCount: 1,
            arrange: chat => chat.AddTurn(ChatTestData.Question, [], ChatTestData.Now + 3 * hour),
            startedAt: ChatTestData.Now);

        var second = await _fixture.SaveChatAsync(
            ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 1, startedAt: ChatTestData.Now + hour);

        var third = await _fixture.SaveChatAsync(
            ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 1, startedAt: ChatTestData.Now + 2 * hour);

        var pageOne = await _fixture.SearchChatsUseCaseAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, limit: 1);

        var pageTwo = await _fixture.SearchChatsUseCaseAsync(
            ChatTestData.OwnerId, ChatTestData.CompanyId, limit: 1, pageOne.NextPosition);

        var pageThree = await _fixture.SearchChatsUseCaseAsync(
            ChatTestData.OwnerId, ChatTestData.CompanyId, limit: 1, pageTwo.NextPosition);

        new[] { pageOne, pageTwo, pageThree }.Select(page => page.Items.Single().Id)
            .Should()
            .Equal(first.Id, third.Id, second.Id);

        pageThree.NextPosition.Should().BeNull();
    }

    [Fact]
    public async Task GetChat_OfTheOwner_ReturnsTheHeaderAndTheMessageCount()
    {
        var chat = await _fixture.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 2);

        var response = await _fixture.GetChatUseCaseAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, chat.Id);

        response.Id.Should().Be(chat.Id);
        response.Title.Should().Be($"{ChatTestData.Question} 0");
        response.MessageCount.Should().Be(expected: 4);
    }

    [Fact]
    public async Task GetChat_OfAnotherUser_ThrowsNotFound()
    {
        var chat = await _fixture.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 1);

        var act = () => _fixture.GetChatUseCaseAsync(ChatTestData.OtherUserId, ChatTestData.CompanyId, chat.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetChat_AfterTheChatIsDeleted_ThrowsNotFound()
    {
        var chat = await _fixture.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 1);
        await _fixture.SoftDeleteChatAsync(chat.Id);

        var act = () => _fixture.GetChatUseCaseAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, chat.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetChatMessages_InPages_ReturnsTheNewestFirstWithoutASkipOrARepeat()
    {
        var chat = await _fixture.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 3);

        var first = await _fixture.GetChatMessagesUseCaseAsync(
            ChatTestData.OwnerId, ChatTestData.CompanyId, chat.Id, ChatTestData.PageLimit);

        var second = await _fixture.GetChatMessagesUseCaseAsync(
            ChatTestData.OwnerId, ChatTestData.CompanyId, chat.Id, ChatTestData.PageLimit, first.NextBefore);

        first.Items.Select(item => item.Sequence).Should().Equal(6, 5, 4, 3);
        first.NextBefore.Should().Be(expected: 3);
        second.Items.Select(item => item.Sequence).Should().Equal(2, 1);
        second.NextBefore.Should().BeNull();
    }

    [Fact]
    public async Task GetChatMessages_ForAMessageWithLinksAndDrafts_ReturnsThem()
    {
        var chat = await _fixture.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 1);

        var response = await _fixture.GetChatMessagesUseCaseAsync(
            ChatTestData.OwnerId, ChatTestData.CompanyId, chat.Id, ChatTestData.PageLimit);

        var reply = response.Items.Single(item => item.Role == ChatMessageRole.Assistant);
        var question = response.Items.Single(item => item.Role == ChatMessageRole.User);
        question.Links.Should().ContainSingle().Which.EntityId.Should().Be(ChatTestData.MachineId);
        reply.Drafts.Should().ContainSingle().Which.DraftId.Should().Be(ChatTestData.DraftId);
        reply.Kind.Should().Be(ChatMessageKind.Answer);
        reply.State.Should().Be(ChatMessageState.Completed);
    }

    [Fact]
    public async Task GetChatMessages_AfterAMessageIsDeleted_LeavesItOut()
    {
        var chat = await _fixture.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 1);
        var reply = chat.Messages.Single(message => message.Role == ChatMessageRole.Assistant);
        await _fixture.SoftDeleteMessageAsync(reply.Id);

        var response = await _fixture.GetChatMessagesUseCaseAsync(
            ChatTestData.OwnerId, ChatTestData.CompanyId, chat.Id, ChatTestData.PageLimit);

        response.Items.Select(item => item.Role).Should().Equal(ChatMessageRole.User);
    }

    [Fact]
    public async Task GetChatMessages_OfAnotherUser_ThrowsNotFound()
    {
        var chat = await _fixture.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 1);

        var act = () => _fixture.GetChatMessagesUseCaseAsync(
            ChatTestData.OtherUserId, ChatTestData.CompanyId, chat.Id, ChatTestData.PageLimit);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
