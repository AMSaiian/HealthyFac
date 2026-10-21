using System.Text.Json.Nodes;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class InboxTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task TryAddAsync_WithNewMessageId_StoresMessageAndReturnsTrue()
    {
        using var scope = fixture.Services.CreateScope();

        var isStored = await scope.ServiceProvider.GetRequiredService<IInbox>()
            .TryAddAsync(
                InboxTestData.NewMessageId,
                InboxTestData.MessageType,
                InboxTestData.Payload,
                InboxTestData.TraceParent);

        var stored = await ReadMessageAsync(InboxTestData.NewMessageId);
        isStored.Should().BeTrue();
        stored.Should().NotBeNull();
        stored!.Type.Should().Be(InboxTestData.MessageType);
        stored.ReceivedOn.Should().Be(InboxTestData.Now);
        stored.NextAttemptOn.Should().Be(InboxTestData.Now);
        stored.ProcessedOn.Should().BeNull();
        stored.TraceParent.Should().Be(InboxTestData.TraceParent);
        JsonNode.DeepEquals(JsonNode.Parse(stored.Payload), JsonNode.Parse(InboxTestData.Payload)).Should().BeTrue();
    }

    [Fact]
    public async Task TryAddAsync_WithDuplicateMessageId_ReturnsFalseAndKeepsOneRow()
    {
        using var firstScope = fixture.Services.CreateScope();
        using var secondScope = fixture.Services.CreateScope();

        var isFirstStored = await firstScope.ServiceProvider.GetRequiredService<IInbox>()
            .TryAddAsync(
                InboxTestData.DuplicateMessageId,
                InboxTestData.MessageType,
                InboxTestData.Payload,
                InboxTestData.TraceParent);

        var isSecondStored = await secondScope.ServiceProvider.GetRequiredService<IInbox>()
            .TryAddAsync(
                InboxTestData.DuplicateMessageId,
                InboxTestData.MessageType,
                InboxTestData.Payload,
                InboxTestData.TraceParent);

        isFirstStored.Should().BeTrue();
        isSecondStored.Should().BeFalse();
        (await CountMessagesAsync(InboxTestData.DuplicateMessageId)).Should().Be(1);
    }

    [Fact]
    public async Task TryAddAsync_InsideRolledBackServiceTransaction_LeavesNoRow()
    {
        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestServiceDbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        await scope.ServiceProvider.GetRequiredService<IInbox>()
            .TryAddAsync(
                InboxTestData.RolledBackMessageId,
                InboxTestData.MessageType,
                InboxTestData.Payload,
                InboxTestData.TraceParent);
        await transaction.RollbackAsync();

        (await CountMessagesAsync(InboxTestData.RolledBackMessageId)).Should().Be(0);
    }

    [Fact]
    public async Task MarkProcessed_WithMessageLoadedInScope_PersistsProcessedOnOnSave()
    {
        using var receiveScope = fixture.Services.CreateScope();
        await receiveScope.ServiceProvider.GetRequiredService<IInbox>()
            .TryAddAsync(
                InboxTestData.ProcessedMessageId,
                InboxTestData.MessageType,
                InboxTestData.Payload,
                InboxTestData.TraceParent);
        using var processScope = fixture.Services.CreateScope();
        var dbContext = processScope.ServiceProvider.GetRequiredService<TestServiceDbContext>();
        var message =
            await dbContext.InboxMessages.SingleAsync(pending => pending.Id == InboxTestData.ProcessedMessageId);

        processScope.ServiceProvider.GetRequiredService<IInbox>().MarkProcessed(message);
        await processScope.ServiceProvider.GetRequiredService<ITestServiceDbContext>().SaveChangesAsync();

        (await ReadMessageAsync(InboxTestData.ProcessedMessageId))!.ProcessedOn.Should().Be(InboxTestData.Now);
    }

    [Fact]
    public async Task MarkProcessed_WhenAnotherScopeCommittedFirst_RejectsSecondCommitWithItsBusinessChange()
    {
        using var receiveScope = fixture.Services.CreateScope();
        await receiveScope.ServiceProvider.GetRequiredService<IInbox>()
            .TryAddAsync(
                InboxTestData.ConcurrentMessageId,
                InboxTestData.MessageType,
                InboxTestData.Payload,
                InboxTestData.TraceParent);
        using var firstScope = fixture.Services.CreateScope();
        using var secondScope = fixture.Services.CreateScope();
        var firstMessage = await LoadMessageAsync(firstScope, InboxTestData.ConcurrentMessageId);
        var secondMessage = await LoadMessageAsync(secondScope, InboxTestData.ConcurrentMessageId);
        firstScope.ServiceProvider.GetRequiredService<IInbox>().MarkProcessed(firstMessage);
        await firstScope.ServiceProvider.GetRequiredService<ITestServiceDbContext>().SaveChangesAsync();
        secondScope.ServiceProvider.GetRequiredService<IOutbox>()
            .Add(new TestBusinessChange { Text = OutboxTestData.BusinessChangeText });
        secondScope.ServiceProvider.GetRequiredService<IInbox>().MarkProcessed(secondMessage);

        var act = () => secondScope.ServiceProvider.GetRequiredService<ITestServiceDbContext>().SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        (await CountOutboxMessagesAsync(OutboxTestData.BusinessChangeMessageType)).Should().Be(0);
    }

    private static Task<InboxMessage> LoadMessageAsync(IServiceScope scope, Guid messageId)
        => scope.ServiceProvider.GetRequiredService<TestServiceDbContext>()
            .InboxMessages
            .SingleAsync(message => message.Id == messageId);

    private async Task<int> CountOutboxMessagesAsync(string messageType)
    {
        using var scope = fixture.Services.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<TestServiceDbContext>()
            .OutboxMessages
            .CountAsync(message => message.Type == messageType);
    }

    private async Task<InboxMessage?> ReadMessageAsync(Guid messageId)
    {
        using var scope = fixture.Services.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<TestServiceDbContext>()
            .InboxMessages
            .AsNoTracking()
            .SingleOrDefaultAsync(message => message.Id == messageId);
    }

    private async Task<int> CountMessagesAsync(Guid messageId)
    {
        using var scope = fixture.Services.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<TestServiceDbContext>()
            .InboxMessages
            .CountAsync(message => message.Id == messageId);
    }
}
