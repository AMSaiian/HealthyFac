using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Chats;
using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Application.Chats.Tools.Reads;
using HealthyFac.Insights.Application.Files;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Insights.IntegrationTests.Chats.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Testing.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Chats;

[Collection(ChatTurnCollection.Name)]
[Feature(TestFeatures.Chat)]
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
            ],
            TestContext.Current.CancellationToken);

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
            [Link(ChatLinkKind.Machine, ChatTestData.MachineId, position: 0)],
            TestContext.Current.CancellationToken);

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

    [Fact]
    public async Task Create_WithACaseOfAMachine_StoresTheLabelWithTheMachineName()
    {
        var caseId = await ArrangeCaseAsync(ChatTestData.CompanyId);

        var response = await _fixture.CreateAsync(
            ChatTestData.OwnerId,
            ChatTestData.Question,
            [Link(ChatLinkKind.MaintenanceCase, caseId, position: 0)],
            TestContext.Current.CancellationToken);

        var labels = await ReadStoredLabelsAsync(response.Chat.Id);
        labels.Should().Equal($"{ChatTestData.MachineName}, case of {ChatTestData.Now:yyyy-MM-dd HH:mm}");
    }

    [Fact]
    public async Task Create_WithACaseWhoseMachineTheUserMayNotRead_StoresTheLabelWithoutTheMachineName()
    {
        var caseId = await ArrangeCaseAsync(ChatTestData.CompanyId);
        _fixture.Fakes.Reads.Failure = new NotFoundException(ChatToolErrors.NotFoundOrNotAllowed);

        var response = await _fixture.CreateAsync(
            ChatTestData.OwnerId,
            ChatTestData.Question,
            [Link(ChatLinkKind.MaintenanceCase, caseId, position: 0)],
            TestContext.Current.CancellationToken);

        var labels = await ReadStoredLabelsAsync(response.Chat.Id);
        labels.Should().Equal($"Case of {ChatTestData.Now:yyyy-MM-dd HH:mm}");
    }

    [Fact]
    public async Task Create_WithACaseOfAnotherCompany_RejectsTheLink()
    {
        var caseId = await ArrangeCaseAsync(ChatTestData.OtherCompanyId);

        var act = () => _fixture.CreateAsync(
            ChatTestData.OwnerId,
            ChatTestData.Question,
            [Link(ChatLinkKind.MaintenanceCase, caseId, position: 0)],
            TestContext.Current.CancellationToken);

        var thrown = await act.Should().ThrowAsync<AppValidationException>();
        thrown.Which.Errors.Select(error => error.PropertyName).Should().Equal("links[0]");
    }

    [Fact]
    public async Task Create_WithACaseAndATokenWithoutCasesRead_RejectsTheLinkAndStoresNothing()
    {
        var caseId = await ArrangeCaseAsync(ChatTestData.CompanyId);
        _fixture.Fakes.Permissions.Granted = [];

        var act = () => _fixture.CreateAsync(
            ChatTestData.OwnerId,
            ChatTestData.Question,
            [Link(ChatLinkKind.MaintenanceCase, caseId, position: 0)],
            TestContext.Current.CancellationToken);

        var thrown = await act.Should().ThrowAsync<AppValidationException>();
        thrown.Which.Errors.Select(error => error.PropertyName).Should().Equal("links[0]");
        var chatCount = await _fixture.CountChatsAsync();
        chatCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task Create_WithAFileTheCatalogReturns_StoresTheFileNameAsTheLabel()
    {
        var fileId = Guid.CreateVersion7();
        _fixture.Inner.Files.Add(File(fileId, name: "Manual.pdf"));

        var response = await _fixture.CreateAsync(
            ChatTestData.OwnerId,
            ChatTestData.Question,
            [Link(ChatLinkKind.File, fileId, position: 0)],
            TestContext.Current.CancellationToken);

        var labels = await ReadStoredLabelsAsync(response.Chat.Id);
        labels.Should().Equal("Manual.pdf");
    }

    [Fact]
    public async Task Create_WithAFileTheCatalogDoesNotReturn_RejectsTheLink()
    {
        var act = () => _fixture.CreateAsync(
            ChatTestData.OwnerId,
            ChatTestData.Question,
            [Link(ChatLinkKind.File, Guid.CreateVersion7(), position: 0)],
            TestContext.Current.CancellationToken);

        var thrown = await act.Should().ThrowAsync<AppValidationException>();
        thrown.Which.Errors.Select(error => error.PropertyName).Should().Equal("links[0]");
    }

    [Fact]
    public async Task Create_WithAMachineLink_GivesTheModelTheContextBlockWithEncryptedIds()
    {
        await _fixture.CreateAsync(
            ChatTestData.OwnerId,
            ChatTestData.Question,
            [Link(ChatLinkKind.Machine, ChatTestData.MachineId, position: 0)],
            TestContext.Current.CancellationToken);

        var encryptedId = _fixture.Inner.GetSingleton<IIdEncryptor>().Encrypt(ChatTestData.MachineId);
        var text = _fixture.Fakes.Model.LastHistory[^1].Text;

        text.Should()
            .StartWith(ChatTestData.Question)
            .And.Contain(expected: "<tool_result>")
            .And.Contain(expected: "\"kind\": \"machine\"");

        text.Should().Contain(encryptedId).And.NotContain(ChatTestData.MachineId.ToString());
    }

    private async Task<Guid> ArrangeCaseAsync(Guid companyId)
    {
        using var scope = _fixture.Inner.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();
        var maintenanceCase = new MaintenanceCase(companyId, ChatTestData.MachineId, ChatTestData.Now);
        dbContext.MaintenanceCases.Add(maintenanceCase);
        await dbContext.SaveChangesAsync();

        return maintenanceCase.Id;
    }

    private static IngestFile File(Guid fileId, string name) =>
        new()
        {
            FileId = fileId,
            CompanyId = ChatTestData.CompanyId,
            MachineChangedAt = ChatTestData.Now,
            Purpose = "attachment",
            FileName = name,
            Extension = "pdf",
            SizeBytes = 10,
        };

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

        return
        [
            .. messages.SelectMany(message => message.Links)
                .OrderBy(link => link.Position)
                .Select(link => link.Label),
        ];
    }

    private async Task<int> CountMessagesAsync(Guid chatId)
    {
        using var scope = _fixture.Inner.ScopeFactory.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
            .ChatMessages
            .CountAsync(message => message.ChatId == chatId);
    }
}
