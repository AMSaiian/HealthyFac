using HealthyFac.Shared.IntegrationTests.Messaging.Data;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class DatabaseOutageDeliveryTests(DatabaseOutageFixture fixture) : IClassFixture<DatabaseOutageFixture>
{
    [Fact]
    public async Task RebusDelivery_WhenDatabaseFailsTransiently_StoresInboxRow()
    {
        await fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.PingPayload,
            DatabaseOutageTestData.OutageMessageId,
            TransportTestData.ActorHeaders());

        var row = await fixture.WaitForInboxMessageAsync(DatabaseOutageTestData.OutageMessageId);
        var errorQueueCount = await RabbitMqQueueReader.CountReadyMessagesAsync(
            fixture.BrokerConnectionString,
            TransportTestData.ErrorQueueName);

        row.Should().NotBeNull();
        errorQueueCount.Should().Be(0);
        fixture.InjectedFailureCount.Should().BeGreaterThanOrEqualTo(DatabaseOutageTestData.FailedCommandCount);
    }
}
