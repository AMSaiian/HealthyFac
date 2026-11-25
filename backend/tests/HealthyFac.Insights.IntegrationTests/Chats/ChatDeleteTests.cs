using HealthyFac.Insights.Application.Chats;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Insights.IntegrationTests.Chats.Data;
using HealthyFac.Insights.IntegrationTests.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Insights.IntegrationTests.Chats;

[Collection(InsightsCollection.Name)]
public sealed class ChatDeleteTests(InsightsFixture fixture)
{
    private const int RaceParticipantCount = 2;

    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task DeleteChat_OfTheOwner_SoftDeletesTheChatItsMessagesLinksAndDrafts()
    {
        var chat = await _fixture.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 2);
        var other = await _fixture.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 1);

        await _fixture.DeleteChatUseCaseAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, chat.Id);

        var stored = await _fixture.LoadChatAsync(chat.Id);
        var otherSequences = await _fixture.ListLiveMessageSequencesAsync(other.Id);
        var deletedLinkCount = await _fixture.CountDeletedLinksAsync();
        var deletedDraftCount = await _fixture.CountDeletedDraftsAsync();

        stored.DeletedAt.Should().NotBeNull();
        stored.Messages.Should().HaveCount(expected: 4).And.OnlyContain(message => message.DeletedAt != null);
        deletedLinkCount.Should().Be(expected: 1);
        deletedDraftCount.Should().Be(expected: 1);
        otherSequences.Should().Equal(1, 2);
    }

    [Fact]
    public async Task DeleteChat_Twice_ThrowsNotFoundTheSecondTime()
    {
        var chat = await _fixture.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 1);
        await _fixture.DeleteChatUseCaseAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, chat.Id);

        var act = () => _fixture.DeleteChatUseCaseAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, chat.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteChat_OfAnotherUser_ThrowsNotFoundAndKeepsTheChat()
    {
        var chat = await _fixture.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 1);

        var act = () => _fixture.DeleteChatUseCaseAsync(ChatTestData.OtherUserId, ChatTestData.CompanyId, chat.Id);

        await act.Should().ThrowAsync<NotFoundException>();
        var stored = await _fixture.LoadChatAsync(chat.Id);
        stored.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task DeleteChatMessage_OfAUserMessage_RemovesItAndItsReplyOnly()
    {
        var chat = await _fixture.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 2);
        var question = chat.Messages.Single(message => message.Sequence == 1);

        await _fixture.DeleteChatMessageUseCaseAsync(
            ChatTestData.OwnerId, ChatTestData.CompanyId, chat.Id, question.Id);

        var sequences = await _fixture.ListLiveMessageSequencesAsync(chat.Id);
        var deletedLinkCount = await _fixture.CountDeletedLinksAsync();
        var stored = await _fixture.LoadChatAsync(chat.Id);

        sequences.Should().Equal(3, 4);
        deletedLinkCount.Should().Be(expected: 1);
        stored.IsSummaryStale.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteChatMessage_OfAnAssistantMessage_ThrowsAValidationFailure()
    {
        var chat = await _fixture.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 1);
        var reply = chat.Messages.Single(message => message.Role == ChatMessageRole.Assistant);

        var act = () => _fixture.DeleteChatMessageUseCaseAsync(
            ChatTestData.OwnerId, ChatTestData.CompanyId, chat.Id, reply.Id);

        await act.Should().ThrowAsync<AppValidationException>();
        var sequences = await _fixture.ListLiveMessageSequencesAsync(chat.Id);
        sequences.Should().Equal(1, 2);
    }

    [Fact]
    public async Task DeleteChatMessage_WhileTheReplyRuns_ThrowsAConflictWithTurnInProgress()
    {
        var chat = await _fixture.SaveChatAsync(
            ChatTestData.OwnerId,
            ChatTestData.CompanyId,
            turnCount: 1,
            arrange: running => running.AddTurn(ChatTestData.Question, [], ChatTestData.Now));
        var runningQuestion = chat.Messages.Single(message => message.Sequence == 3);

        var act = () => _fixture.DeleteChatMessageUseCaseAsync(
            ChatTestData.OwnerId, ChatTestData.CompanyId, chat.Id, runningQuestion.Id);

        await act.Should().ThrowAsync<ConflictException>().WithMessage(ChatErrors.TurnInProgress);
        var sequences = await _fixture.ListLiveMessageSequencesAsync(chat.Id);
        sequences.Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public async Task DeleteChatMessage_InsideTheSummary_MarksTheSummaryStale()
    {
        var chat = await _fixture.SaveChatAsync(
            ChatTestData.OwnerId,
            ChatTestData.CompanyId,
            turnCount: 2,
            arrange: summarized => summarized.SetSummary(text: "The spindle runs hot.", throughSequence: 2));
        var covered = chat.Messages.Single(message => message.Sequence == 1);
        var outside = chat.Messages.Single(message => message.Sequence == 3);

        await _fixture.DeleteChatMessageUseCaseAsync(
            ChatTestData.OwnerId, ChatTestData.CompanyId, chat.Id, outside.Id);
        var afterOutside = await _fixture.LoadChatAsync(chat.Id);
        await _fixture.DeleteChatMessageUseCaseAsync(
            ChatTestData.OwnerId, ChatTestData.CompanyId, chat.Id, covered.Id);
        var afterCovered = await _fixture.LoadChatAsync(chat.Id);

        afterOutside.IsSummaryStale.Should().BeFalse();
        afterCovered.IsSummaryStale.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteChatMessage_OfTwoMessagesAtTheSameTime_ConflictsForTheSecond()
    {
        var chat = await _fixture.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 2);
        var first = chat.Messages.Single(message => message.Sequence == 1);
        var second = chat.Messages.Single(message => message.Sequence == 3);
        using var race = _fixture.SaveGate.StartRace(RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.DeleteChatMessageUseCaseAsync(
                ChatTestData.OwnerId, ChatTestData.CompanyId, chat.Id, first.Id)),
            race.RunAsync(() => _fixture.DeleteChatMessageUseCaseAsync(
                ChatTestData.OwnerId, ChatTestData.CompanyId, chat.Id, second.Id)));

        var sequences = await _fixture.ListLiveMessageSequencesAsync(chat.Id);

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null).Should().BeOfType<ConflictException>();
        sequences.Should().HaveCount(expected: 2);
    }
}
