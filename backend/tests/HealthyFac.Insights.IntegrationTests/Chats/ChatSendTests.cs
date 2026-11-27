using HealthyFac.Insights.Application.Chats;
using HealthyFac.Insights.Application.Chats.Tools.Reads;
using HealthyFac.Insights.Application.Chats.Turns;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.Domain.Chats;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Insights.IntegrationTests.Chats.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.IntegrationTests.Chats;

[Collection(ChatTurnCollection.Name)]
[Feature(TestFeatures.Chat)]
public sealed class ChatSendTests(ChatTurnFixture fixture)
{
    private const int RaceParticipantCount = 2;

    private readonly ChatTurnFixture _fixture = fixture;

    [Fact]
    public async Task Create_WithAnInScopeText_StoresTheChatAndReturnsTheCompletedReply()
    {
        var response = await _fixture.CreateAsync(
            ChatTestData.OwnerId,
            ChatTestData.Question,
            [ChatSendTestData.Link(ChatTestData.MachineId)],
            TestContext.Current.CancellationToken);

        var states = await _fixture.ListMessageStatesAsync(response.Chat.Id);

        response.Chat.Title.Should().Be(ChatTestData.Question);
        response.User.Links.Should().ContainSingle().Which.EntityId.Should().Be(ChatTestData.MachineId);
        response.Assistant.State.Should().Be(ChatMessageState.Completed);
        response.Assistant.Text.Should().Be(ChatTestData.Answer);
        states.Should().Equal((1, ChatMessageState.Completed), (2, ChatMessageState.Completed));
    }

    [Theory]
    [MemberData(nameof(ChatSendTestData.InvalidInputs), MemberType = typeof(ChatSendTestData))]
    public async Task Create_WithAnInvalidInput_ThrowsAValidationFailureAndStoresNothing(
        string caseName,
        string text,
        IReadOnlyList<ChatLinkInput> links,
        string expectedProperty
    )
    {
        var act = () => _fixture.CreateAsync(ChatTestData.OwnerId, text, links);

        var thrown = await act.Should().ThrowAsync<AppValidationException>(caseName);
        thrown.Which.Errors.Select(error => error.PropertyName).Distinct().Should().Equal(expectedProperty);
        var chatCount = await _fixture.CountChatsAsync();
        chatCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task Send_ToABlockedChat_ThrowsUnprocessableWithChatBlocked()
    {
        var chat = await _fixture.Inner.SaveChatAsync(
            ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 1, arrange: blocked => blocked.Block());

        var act = () => _fixture.SendAsync(ChatTestData.OwnerId, chat.Id, ChatTestData.Question);

        await act.Should().ThrowAsync<UnprocessableException>().WithMessage(ChatErrors.ChatBlocked);
    }

    [Fact]
    public async Task Send_ToAChatOfAnotherUser_ThrowsNotFound()
    {
        var chat = await _fixture.Inner.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 1);

        var act = () => _fixture.SendAsync(ChatTestData.OtherUserId, chat.Id, ChatTestData.Question);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Send_WhileAReplyStreamsForLessThanTheLimit_ThrowsAConflictWithTurnInProgress()
    {
        var chat = await _fixture.Inner.SaveChatAsync(
            ChatTestData.OwnerId,
            ChatTestData.CompanyId,
            turnCount: 1,
            arrange: running => running.AddTurn(ChatTestData.Question, [], _fixture.Time.GetUtcNow().UtcDateTime));

        _fixture.Time.Advance(TimeSpan.FromSeconds(seconds: 30));

        var act = () => _fixture.SendAsync(ChatTestData.OwnerId, chat.Id, ChatTestData.Question);

        await act.Should().ThrowAsync<ConflictException>().WithMessage(ChatErrors.TurnInProgress);
        var states = await _fixture.ListMessageStatesAsync(chat.Id);
        states.Should().HaveCount(expected: 4);
    }

    [Fact]
    public async Task Send_AfterAReplyStreamedLongerThanTheLimit_InterruptsItAndStoresTheNewTurn()
    {
        var chat = await _fixture.Inner.SaveChatAsync(
            ChatTestData.OwnerId,
            ChatTestData.CompanyId,
            turnCount: 1,
            arrange: running => running.AddTurn(ChatTestData.Question, [], _fixture.Time.GetUtcNow().UtcDateTime));

        _fixture.Time.Advance(TimeSpan.FromSeconds(seconds: 100));

        var response = await _fixture.SendAsync(ChatTestData.OwnerId, chat.Id, ChatTestData.Question);

        var states = await _fixture.ListMessageStatesAsync(chat.Id);
        response.Assistant.State.Should().Be(ChatMessageState.Completed);

        states.Select(state => state.State)
            .Should()
            .Equal(
                ChatMessageState.Completed,
                ChatMessageState.Completed,
                ChatMessageState.Completed,
                ChatMessageState.Interrupted,
                ChatMessageState.Completed,
                ChatMessageState.Completed);
    }

    [Fact]
    public async Task Send_TwiceAtTheSameTime_StoresOneTurnAndConflictsForTheOther()
    {
        var chat = await _fixture.Inner.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 1);
        using var race = _fixture.Inner.SaveGate.StartRace(RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.SendAsync(ChatTestData.OwnerId, chat.Id, ChatTestData.Question)),
            race.RunAsync(() => _fixture.SendAsync(ChatTestData.OwnerId, chat.Id, ChatTestData.Question)));

        var states = await _fixture.ListMessageStatesAsync(chat.Id);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null).Should().BeOfType<ConflictException>();
        states.Should().HaveCount(expected: 4);
        _fixture.Fakes.Model.CallCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task Create_ForTwoChatsOfOneUserAtTheSameTime_RunsBothTurnsTogether()
    {
        _fixture.Fakes.Model.ConcurrentStreamCount = 2;

        var responses = await Task.WhenAll(
            _fixture.CreateAsync(
                ChatTestData.OwnerId, ChatTestData.Question, cancellationToken: TestContext.Current.CancellationToken),
            _fixture.CreateAsync(
                ChatTestData.OwnerId, ChatTestData.Question, cancellationToken: TestContext.Current.CancellationToken));

        responses.Select(response => response.Assistant.State)
            .Should()
            .OnlyContain(state => state == ChatMessageState.Completed);

        responses.Select(response => response.Chat.Id).Distinct().Should().HaveCount(expected: 2);
    }

    [Fact]
    public async Task Create_ForAMaliciousText_BlocksTheChat()
    {
        _fixture.Fakes.Triage.Choice = ChatTriageChoice.Malicious;

        var response = await _fixture.CreateAsync(
            ChatTestData.OwnerId, ChatTestData.Question, cancellationToken: TestContext.Current.CancellationToken);

        response.Chat.State.Should().Be(ChatState.Blocked);
        response.Assistant.Kind.Should().Be(ChatMessageKind.BlockedNotice);
    }

    [Fact]
    public async Task Create_WhenTheModelCallsATool_StoresTheRunAndTheCallRowInTheFinalSave()
    {
        _fixture.Fakes.Model.ToolName = "search_machines";
        _fixture.Fakes.Reads.MachinesAnswer = new SearchMachinesResponse { TotalCount = 0, Machines = [] };

        var response = await _fixture.CreateAsync(
            ChatTestData.OwnerId, ChatTestData.Question, [], TestContext.Current.CancellationToken);

        response.Assistant.State.Should().Be(ChatMessageState.Completed);
        var calls = await _fixture.ReadAllToolCallsAsync();
        var call = calls.Should().ContainSingle().Subject;
        call.ToolName.Should().Be(expected: "search_machines");
        call.Outcome.Should().Be(ToolCallOutcome.Succeeded);
        var runCount = await _fixture.CountRunsAsync(call.RunId);
        runCount.Should().Be(expected: 1);
    }
}
