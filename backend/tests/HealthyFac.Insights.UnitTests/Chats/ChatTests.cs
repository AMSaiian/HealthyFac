using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Chats;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Insights.UnitTests.Chats.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.UnitTests.Chats;

[Feature(TestFeatures.Chat)]
public sealed class ChatTests
{
    [Fact]
    public void AddTurn_WithATextLongerThanTheTitleLimit_CutsTheTitleOfTheFirstTurnOnly()
    {
        var chat = CreateChat();
        var longText = new string(c: 'a', ChatTestData.LongTextLength);

        chat.AddTurn(longText, [], ChatTestData.Now);
        chat.AddTurn(ChatTestData.ShortText, [], ChatTestData.Now);

        chat.Title.Should().Be(new string(c: 'a', ChatTestData.TitleLength));
    }

    [Fact]
    public void AddTurn_WithAnEmojiAtTheTitleLimit_CutsBeforeTheSurrogatePair()
    {
        var chat = CreateChat();

        chat.AddTurn(ChatTestData.EmojiAtTitleLimit, [], ChatTestData.Now);

        chat.Title.Should().Be(new string(c: 'a', ChatTestData.TitleLength - 1));
    }

    [Fact]
    public void AddTurn_Twice_GivesEachMessageTheNextSequenceAndStartsTheReplyStreaming()
    {
        var chat = CreateChat();

        var (firstUser, firstAssistant) = chat.AddTurn(
            ChatTestData.ShortText, ChatTestData.Links, ChatTestData.Now);

        var (secondUser, secondAssistant) = chat.AddTurn(ChatTestData.ShortText, [], ChatTestData.Now);

        new[] { firstUser.Sequence, firstAssistant.Sequence, secondUser.Sequence, secondAssistant.Sequence }
            .Should()
            .Equal(1, 2, 3, 4);

        firstUser.State.Should().Be(ChatMessageState.Completed);
        firstUser.Links.Should().ContainSingle();
        firstAssistant.State.Should().Be(ChatMessageState.Streaming);
        firstAssistant.Text.Should().BeEmpty();
        chat.NextSequence.Should().Be(expected: 5);
    }

    [Fact]
    public void AddTurn_ToABlockedChat_Throws()
    {
        var chat = CreateChat();
        chat.Block();

        var act = () => chat.AddTurn(ChatTestData.ShortText, [], ChatTestData.Now);

        act.Should().Throw<InvalidOperationException>();
        chat.Messages.Should().BeEmpty();
    }

    [Fact]
    public void DeleteUserMessage_OfACompletedTurn_ReturnsTheMessageAndItsReply()
    {
        var chat = CreateChat();
        var (user, assistant) = chat.AddTurn(ChatTestData.ShortText, [], ChatTestData.Now);
        assistant.Complete(ChatMessageKind.Answer, text: "It needs oil.");

        var removed = chat.DeleteUserMessage(user.Id, ChatTestData.Now);

        removed.Select(message => message.Id).Should().Equal(user.Id, assistant.Id);
        chat.IsSummaryStale.Should().BeFalse();
    }

    [Fact]
    public void DeleteUserMessage_InsideTheSummary_MarksTheSummaryStale()
    {
        var chat = CreateChat();
        var (coveredUser, coveredAssistant) = chat.AddTurn(ChatTestData.ShortText, [], ChatTestData.Now);
        coveredAssistant.Complete(ChatMessageKind.Answer, text: "It needs oil.");
        var (outsideUser, outsideAssistant) = chat.AddTurn(ChatTestData.ShortText, [], ChatTestData.Now);
        outsideAssistant.Complete(ChatMessageKind.Answer, text: "Check the filter.");
        chat.SetSummary(ChatTestData.Summary, coveredAssistant.Sequence);

        chat.DeleteUserMessage(outsideUser.Id, ChatTestData.Now);
        var staleAfterOutside = chat.IsSummaryStale;
        chat.DeleteUserMessage(coveredUser.Id, ChatTestData.Now);

        staleAfterOutside.Should().BeFalse();
        chat.IsSummaryStale.Should().BeTrue();
    }

    [Fact]
    public void DeleteUserMessage_OfAnAssistantMessage_Throws()
    {
        var chat = CreateChat();
        var (_, assistant) = chat.AddTurn(ChatTestData.ShortText, [], ChatTestData.Now);
        assistant.Complete(ChatMessageKind.Answer, text: "It needs oil.");

        var act = () => chat.DeleteUserMessage(assistant.Id, ChatTestData.Now);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DeleteUserMessage_WhileTheReplyStreams_Throws()
    {
        var chat = CreateChat();
        var (user, _) = chat.AddTurn(ChatTestData.ShortText, [], ChatTestData.Now);

        var act = () => chat.DeleteUserMessage(user.Id, ChatTestData.Now);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_AfterTheReplyEnded_Throws()
    {
        var chat = CreateChat();
        var reply = chat.AddTurn(ChatTestData.ShortText, [], ChatTestData.Now).Assistant;
        reply.End(ChatMessageState.Cancelled, errorCode: null);

        var act = () => reply.Complete(ChatMessageKind.Answer, text: "Late answer.");

        act.Should().Throw<InvalidOperationException>();
        reply.State.Should().Be(ChatMessageState.Cancelled);
    }

    [Theory]
    [MemberData(nameof(ChatTestData.FinalStates), MemberType = typeof(ChatTestData))]
    public void End_WithAFinalState_StoresTheStateAndTheErrorAndLeavesNoWayBack(
        string caseName,
        ChatMessageState state,
        AgentRunErrorCode? errorCode
    )
    {
        var reply = CreateChat().AddTurn(ChatTestData.ShortText, [], ChatTestData.Now).Assistant;

        reply.End(state, errorCode);
        var again = () => reply.End(ChatMessageState.Failed, AgentRunErrorCode.UnexpectedError);

        reply.State.Should().Be(state, caseName);
        reply.ErrorCode.Should().Be(errorCode!);
        again.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [MemberData(nameof(ChatTestData.StatesThatDoNotEndAReply), MemberType = typeof(ChatTestData))]
    public void End_WithAStateThatDoesNotEndAReply_Throws(string caseName, ChatMessageState state)
    {
        var reply = CreateChat().AddTurn(ChatTestData.ShortText, [], ChatTestData.Now).Assistant;

        var act = () => reply.End(state, errorCode: null);

        act.Should().Throw<InvalidOperationException>(caseName);
    }

    [Theory]
    [MemberData(nameof(ChatTestData.DeadChecks), MemberType = typeof(ChatTestData))]
    public void IsDead_ForAReplyThatStreams_IsTrueOnlyAboveTheTurnLimit(string caseName, int seconds, bool isDead)
    {
        var reply = CreateChat().AddTurn(ChatTestData.ShortText, [], ChatTestData.Now).Assistant;

        var result = reply.IsDead(ChatTestData.Now.AddSeconds(seconds), ChatTestData.TurnLimit);

        result.Should().Be(isDead, caseName);
    }

    [Fact]
    public void IsDead_ForAReplyThatEnded_IsFalse()
    {
        var reply = CreateChat().AddTurn(ChatTestData.ShortText, [], ChatTestData.Now).Assistant;
        reply.Complete(ChatMessageKind.Answer, text: "It needs oil.");

        var result = reply.IsDead(ChatTestData.Now.AddHours(value: 1), ChatTestData.TurnLimit);

        result.Should().BeFalse();
    }

    private static Chat CreateChat() => new(ChatTestData.OwnerId, ChatTestData.CompanyId, ChatTestData.Now);
}
