using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Chats;
using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Application.Chats.Tools.Reads;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Insights.IntegrationTests.Chats.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Chats;

[Collection(ChatTurnCollection.Name)]
public sealed class ChatLinkTests(ChatTurnFixture fixture)
{
    private static readonly Guid EmployeeId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000f31");
    private static readonly Guid WorkOrderId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000f32");

    private readonly ChatTurnFixture _fixture = fixture;

    [Fact]
    public async Task Create_WithLinksOfThreeKinds_StoresTheLabelsThatTheReadsGave()
    {
        _fixture.Fakes.Reads.EmployeeAnswer = new ChatEmployee { Id = EmployeeId, DisplayName = "Ann Brown" };
        _fixture.Fakes.Reads.WorkOrderAnswer = WorkOrder(title: "Replace the filter");

        var response = await _fixture.CreateAsync(
            ChatTestData.OwnerId,
            ChatTestData.Question,
            [
                Link(ChatLinkKind.Machine, ChatTestData.MachineId, position: 0),
                Link(ChatLinkKind.Employee, EmployeeId, position: 1),
                Link(ChatLinkKind.WorkOrder, WorkOrderId, position: 2),
            ]);

        var labels = await ReadStoredLabelsAsync(response.Chat.Id);
        labels.Should().Equal(ChatTestData.MachineName, "Ann Brown", "Replace the filter");
    }

    [Fact]
    public async Task Create_WithAMachineThatTheUserMayNotRead_ThrowsTheLinkKeyAndStoresNothing()
    {
        _fixture.Fakes.Reads.Failure = new NotFoundException(ChatToolErrors.NotFoundOrNotAllowed);

        var act = () => _fixture.CreateAsync(
            ChatTestData.OwnerId,
            ChatTestData.Question,
            [Link(ChatLinkKind.Machine, ChatTestData.MachineId, position: 0)]);

        var thrown = await act.Should().ThrowAsync<AppValidationException>();
        thrown.Which.Errors.Select(error => error.PropertyName).Should().Equal("links[0]");
        var chatCount = await _fixture.CountChatsAsync();
        chatCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task Send_WithAWorkOrderOfAnotherEmployee_RejectsTheLinkAndStoresNoMessage()
    {
        var chat = await _fixture.Inner.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 1);
        _fixture.Fakes.Reads.Failure = new NotFoundException(ChatToolErrors.NotFoundOrNotAllowed);

        var act = () => _fixture.SendAsync(
            ChatTestData.OwnerId,
            chat.Id,
            ChatTestData.Question,
            [Link(ChatLinkKind.WorkOrder, WorkOrderId, position: 0)]);

        var thrown = await act.Should().ThrowAsync<AppValidationException>();
        thrown.Which.Errors.Select(error => error.PropertyName).Should().Equal("links[0]");
        var messageCount = await CountMessagesAsync(chat.Id);
        messageCount.Should().Be(expected: 2);
    }

    [Fact]
    public async Task Send_WithAnEmployeeThatTheCompanyDoesNotHave_RejectsTheLinkWithItsIndex()
    {
        var chat = await _fixture.Inner.SaveChatAsync(ChatTestData.OwnerId, ChatTestData.CompanyId, turnCount: 1);
        _fixture.Fakes.Reads.EmployeeAnswer = null;

        var act = () => _fixture.SendAsync(
            ChatTestData.OwnerId,
            chat.Id,
            ChatTestData.Question,
            [
                Link(ChatLinkKind.Machine, ChatTestData.MachineId, position: 0),
                Link(ChatLinkKind.Employee, EmployeeId, position: 1),
            ]);

        var thrown = await act.Should().ThrowAsync<AppValidationException>();
        thrown.Which.Errors.Select(error => error.PropertyName).Should().Equal("links[1]");
    }

    private static ChatLinkInput Link(ChatLinkKind kind, Guid entityId, int position) =>
        new() { Kind = kind, EntityId = entityId, Position = position };

    private static GetWorkOrderResponse WorkOrder(string title) =>
        new()
        {
            Id = WorkOrderId,
            Title = title,
            Status = "backlog",
            Priority = "normal",
            Category = "repair",
            EstimateMinutes = 30,
            IsDelayed = false,
            CreatedAt = ChatTestData.Now,
            History = [],
        };

    private async Task<IReadOnlyList<string>> ReadStoredLabelsAsync(Guid chatId)
    {
        using var scope = _fixture.Inner.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();
        var messages = await dbContext.ChatMessages
            .Include(message => message.Links)
            .Where(message => message.ChatId == chatId)
            .ToListAsync();

        return messages.SelectMany(message => message.Links)
            .OrderBy(link => link.Position)
            .Select(link => link.Label)
            .ToList();
    }

    private async Task<int> CountMessagesAsync(Guid chatId)
    {
        using var scope = _fixture.Inner.ScopeFactory.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
            .ChatMessages
            .CountAsync(message => message.ChatId == chatId);
    }
}
