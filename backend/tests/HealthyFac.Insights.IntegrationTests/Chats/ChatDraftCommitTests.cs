using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Insights.IntegrationTests.Chats.Data;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Insights.Messaging.WorkOrders;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.RichText;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Chats;

[Collection(ChatTurnCollection.Name)]
public sealed class ChatDraftCommitTests(ChatTurnFixture fixture)
{
    private readonly ChatTurnFixture _fixture = fixture;

    [Fact]
    public async Task Create_WithACompletedTurnThatProposed_StoresCommittedRowsAndSendsOneMessagePerGroup()
    {
        ScriptProposals();

        var response = await _fixture.CreateAsync(ChatTestData.OwnerId, ChatTestData.Question);

        var drafts = await ReadDraftsAsync(response.Assistant.Id);
        var proposed = await _fixture.ReadOutboxAsync<DraftsProposed>();
        var workOrders = await _fixture.ReadOutboxAsync<WorkOrderDraftProposed>();
        drafts.Select(draft => draft.Kind)
            .Should()
            .BeEquivalentTo(
                [ChatDraftKind.Machine, ChatDraftKind.Metric, ChatDraftKind.Metric, ChatDraftKind.WorkOrder]);
        drafts.Should().OnlyContain(draft => draft.IsCommitted);
        var message = proposed.Should().ContainSingle().Subject;
        var richTextPolicy = _fixture.Inner.GetSingleton<IRichTextPolicy>();
        new DraftsProposedValidator(richTextPolicy).Validate(message).IsValid.Should().BeTrue();
        message.Source.Should().Be(ProposalSource.Chat);
        message.ChatId.Should().Be(response.Chat.Id);
        message.ChatMessageId.Should().Be(response.Assistant.Id);
        message.Machines.Should().ContainSingle().Which.TargetMachineId.Should().Be(ChatTestData.MachineId);
        message.Metrics.Should().HaveCount(expected: 2);
        var workOrder = workOrders.Should().ContainSingle().Subject;
        new WorkOrderDraftProposedValidator().Validate(workOrder).IsValid.Should().BeTrue();
        workOrder.Source.Should().Be(WorkOrderProposalSource.Chat);
        workOrder.ChatMessageId.Should().Be(response.Assistant.Id);
    }

    [Fact]
    public async Task Create_WithACompletedTurnThatProposedNothing_SendsNoMessage()
    {
        var response = await _fixture.CreateAsync(ChatTestData.OwnerId, ChatTestData.Question);

        var drafts = await ReadDraftsAsync(response.Assistant.Id);
        var proposed = await _fixture.ReadOutboxAsync<DraftsProposed>();
        var workOrders = await _fixture.ReadOutboxAsync<WorkOrderDraftProposed>();
        drafts.Should().BeEmpty();
        proposed.Should().BeEmpty();
        workOrders.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_WithATurnThatFailedAfterItProposed_StoresUncommittedRowsAndSendsNothing()
    {
        ScriptProposals();
        _fixture.Fakes.Model.FailureAfterTools = new InvalidOperationException(message: "The provider failed.");

        var response = await _fixture.CreateAsync(ChatTestData.OwnerId, ChatTestData.Question);

        var drafts = await ReadDraftsAsync(response.Assistant.Id);
        var proposed = await _fixture.ReadOutboxAsync<DraftsProposed>();
        var workOrders = await _fixture.ReadOutboxAsync<WorkOrderDraftProposed>();
        response.Assistant.State.Should().Be(ChatMessageState.Failed);
        drafts.Should().HaveCount(expected: 4);
        drafts.Should().OnlyContain(draft => !draft.IsCommitted);
        proposed.Should().BeEmpty();
        workOrders.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_WithACancelledTurnThatProposed_StoresUncommittedRowsAndSendsNothing()
    {
        ScriptProposals();
        using var source = new CancellationTokenSource();
        _fixture.Fakes.Model.CancelSource = source;

        var response = await _fixture.CreateAsync(
            ChatTestData.OwnerId, ChatTestData.Question, cancellationToken: source.Token);

        var drafts = await ReadDraftsAsync(response.Assistant.Id);
        var proposed = await _fixture.ReadOutboxAsync<DraftsProposed>();
        var workOrders = await _fixture.ReadOutboxAsync<WorkOrderDraftProposed>();
        response.Assistant.State.Should().Be(ChatMessageState.Cancelled);
        drafts.Should().HaveCount(expected: 4);
        drafts.Should().OnlyContain(draft => !draft.IsCommitted);
        proposed.Should().BeEmpty();
        workOrders.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_WithATurnThatProposedThenReportedNoAnswer_StoresNoDraftRowsAndSendsNothing()
    {
        _fixture.Fakes.Model.ToolCalls.Add(("propose_machine_draft", """{"name":"Lathe 9"}"""));
        _fixture.Fakes.Model.ToolCalls.Add(("report_no_answer", "{}"));

        var response = await _fixture.CreateAsync(ChatTestData.OwnerId, ChatTestData.Question);

        var drafts = await ReadDraftsAsync(response.Assistant.Id);
        var proposed = await _fixture.ReadOutboxAsync<DraftsProposed>();
        var toolCalls = await _fixture.ReadAllToolCallsAsync();
        response.Assistant.Kind.Should().Be(ChatMessageKind.Unanswerable);
        drafts.Should().BeEmpty();
        proposed.Should().BeEmpty();
        toolCalls.Select(call => call.ToolName).Should().Contain(["propose_machine_draft", "report_no_answer"]);
    }

    private void ScriptProposals()
    {
        var encryptedMachine = _fixture.Inner.GetSingleton<IIdEncryptor>().Encrypt(ChatTestData.MachineId);
        var model = _fixture.Fakes.Model;
        model.ToolCalls.Add(
            ("propose_machine_draft", $$"""{"targetMachineId":"{{encryptedMachine}}","name":"Lathe 7"}"""));
        model.ToolCalls.Add(
            ("propose_metric_draft", $$"""{"machineId":"{{encryptedMachine}}","key":"temp","valueKind":"numeric"}"""));
        model.ToolCalls.Add(
            ("propose_metric_draft", $$"""{"machineId":"{{encryptedMachine}}","key":"load","valueKind":"numeric"}"""));
        model.ToolCalls.Add(
            (
                "propose_work_order_draft",
                """{"title":"Replace the filter","priority":"high","category":"repair","estimateMinutes":30}"""));
    }

    private async Task<IReadOnlyList<ChatMessageDraft>> ReadDraftsAsync(Guid messageId)
    {
        using var scope = _fixture.Inner.ScopeFactory.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<InsightsDbContext>()
            .ChatMessages
            .AsNoTracking()
            .Where(message => message.Id == messageId)
            .SelectMany(message => message.Drafts)
            .ToListAsync();
    }
}
