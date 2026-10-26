using System.Text.Json.Nodes;
using HealthyFac.Shared.Infrastructure.Messaging.Processing;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;
using LinqToDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class OutboxProcessorTests(TransportFixture fixture)
    : IClassFixture<TransportFixture>, IDisposable
{
    private readonly TestSubscriber _subscriber =
        new(fixture.BrokerConnectionString, OutboxProcessorTestData.SubscriberQueueName);

    [Fact]
    public async Task OutboxProcessor_WithPendingRow_PublishesAndMarksProcessed()
    {
        await _subscriber.SubscribeAsync(OutboxProcessorTestData.PublishedMessageType);
        await fixture.AddPendingOutboxRowsAsync(
            OutboxProcessorTestData.PublishedMessageType,
            OutboxProcessorTestData.SingleRowCount);
        var processor = fixture.Create<OutboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var received = await _subscriber.WaitForMessagesAsync(
            OutboxProcessorTestData.PublishedMessageType,
            OutboxProcessorTestData.SingleRowCount);
        var rows = await fixture.ReadOutboxRowsAsync(OutboxProcessorTestData.PublishedMessageType);

        var row = rows.Should().ContainSingle().Which;
        var message = received.Should().ContainSingle().Which;
        row.ProcessedOn.Should().NotBeNull();
        message.Id.Should().Be(row.Id);
        JsonNode.DeepEquals(JsonNode.Parse(message.Payload), JsonNode.Parse(OutboxProcessorTestData.Payload))
            .Should()
            .BeTrue();
        message.Headers[MessageHeaders.ActorId].Should().Be(TransportTestData.ActorId.ToString());
        message.Headers[MessageHeaders.CompanyId].Should().Be(TransportTestData.CompanyId.ToString());
    }

    [Fact]
    public async Task OutboxProcessor_TwoInstances_PublishEachRowOnce()
    {
        await _subscriber.SubscribeAsync(OutboxProcessorTestData.ConcurrentMessageType);
        await fixture.AddPendingOutboxRowsAsync(
            OutboxProcessorTestData.ConcurrentMessageType,
            OutboxProcessorTestData.ConcurrentRowCount);
        var firstProcessor = fixture.Create<OutboxProcessor>();
        var secondProcessor = fixture.Create<OutboxProcessor>();

        await Task.WhenAll(
            firstProcessor.RunAsync(CancellationToken.None),
            secondProcessor.RunAsync(CancellationToken.None));

        var received = await _subscriber.WaitForMessagesAsync(
            OutboxProcessorTestData.ConcurrentMessageType,
            OutboxProcessorTestData.ConcurrentRowCount);
        var rows = await fixture.ReadOutboxRowsAsync(OutboxProcessorTestData.ConcurrentMessageType);

        received.Select(message => message.Id).Should().OnlyHaveUniqueItems();
        received.Should().HaveCount(OutboxProcessorTestData.ConcurrentRowCount);
        rows.Should().OnlyContain(row => row.ProcessedOn != null);
    }

    [Fact]
    public async Task ClaimQuery_InsideTransaction_HoldsRowLockAgainstSecondClaim()
    {
        await fixture.AddPendingOutboxRowsAsync(
            OutboxProcessorTestData.LockedMessageType,
            OutboxProcessorTestData.SingleRowCount);
        using var firstScope = fixture.CreateScope();
        using var secondScope = fixture.CreateScope();
        var firstContext = firstScope.ServiceProvider.GetRequiredService<TestServiceDbContext>();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<TestServiceDbContext>();

        var claimedIds = await firstContext.RunInTransactionAsync(async token =>
        {
            var firstClaim = await firstContext.ClaimNextOutboxMessageAsync(InboxTestData.Now, token);
            var secondClaim = await secondContext.RunInTransactionAsync(
                secondToken => secondContext.ClaimNextOutboxMessageAsync(InboxTestData.Now, secondToken),
                token);

            return (First: firstClaim?.Id, Second: secondClaim?.Id);
        });

        claimedIds.First.Should().NotBeNull();
        claimedIds.Second.Should().NotBe(claimedIds.First!.Value);
    }

    [Fact]
    public async Task ClaimQuery_GeneratedSql_EndsWithForUpdateSkipLocked()
    {
        using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestServiceDbContext>();

        var sql = dbContext.BuildOutboxClaimQuery(InboxTestData.Now).ToSqlQuery().Sql;

        sql.Should().Contain(OutboxProcessorTestData.ForUpdateSkipLockedSql);
    }

    [Fact]
    public async Task ClaimQuery_WhenTableIsMissing_ThrowsPostgresExceptionUnchanged()
    {
        using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestServiceDbContext>();

        var act = () => dbContext.RunInTransactionAsync(async token =>
        {
            await dbContext.Database.ExecuteSqlRawAsync(OutboxProcessorTestData.DropOutboxTableSql, token);
            await dbContext.ClaimNextOutboxMessageAsync(InboxTestData.Now, token);
        });

        await act.Should().ThrowAsync<PostgresException>();
    }

    public void Dispose() => _subscriber.Dispose();
}
