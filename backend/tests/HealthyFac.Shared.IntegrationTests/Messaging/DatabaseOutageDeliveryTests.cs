using HealthyFac.Shared.IntegrationTests.Messaging.Data;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class DatabaseOutageDeliveryTests(DatabaseOutageFixture fixture) : IClassFixture<DatabaseOutageFixture>
{
    private readonly DatabaseOutageFixture _fixture = fixture;

    [Fact]
    public async Task RebusDelivery_WhenDatabaseFailsTransiently_StoresInboxRow()
    {
        await _fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.PingPayload,
            DatabaseOutageTestData.OutageMessageId,
            TransportTestData.ActorHeaders());

        var row = await _fixture.WaitForInboxMessageAsync(DatabaseOutageTestData.OutageMessageId);
        var errorQueueCount = await RabbitMqQueueReader.CountReadyMessagesAsync(
            _fixture.BrokerConnectionString,
            TransportTestData.ErrorQueueName);

        row.Should().NotBeNull();
        errorQueueCount.Should().Be(expected: 0);
        _fixture.InjectedFailureCount.Should().BeGreaterThanOrEqualTo(DatabaseOutageTestData.FailedCommandCount);
    }
}
