using System.Text.Json.Nodes;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class TransportTests(TransportFixture fixture) : IClassFixture<TransportFixture>
{
    [Fact]
    public async Task RebusDelivery_WithActorHeaders_StoresInboxRowWithActor()
    {
        await fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.PingPayload,
            TransportTestData.DeliveredMessageId,
            TransportTestData.ActorHeaders());

        var stored = await fixture.WaitForInboxMessageAsync(TransportTestData.DeliveredMessageId);

        stored.Should().NotBeNull();
        stored!.Type.Should().Be(TransportTestData.PingMessageType);
        stored.ActorId.Should().Be(TransportTestData.ActorId);
        stored.CompanyId.Should().Be(TransportTestData.CompanyId);
        JsonNode.DeepEquals(JsonNode.Parse(stored.Payload), JsonNode.Parse(TransportTestData.PingPayload))
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task RebusDelivery_SameMessageIdTwice_HandlesOnce()
    {
        await fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.PingPayload,
            TransportTestData.DuplicatedMessageId,
            TransportTestData.ActorHeaders());
        await fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.PingPayload,
            TransportTestData.DuplicatedMessageId,
            TransportTestData.ActorHeaders());
        await fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.PingPayload,
            TransportTestData.SentinelMessageId,
            TransportTestData.ActorHeaders());

        await fixture.WaitForInboxMessageAsync(TransportTestData.SentinelMessageId);
        await fixture.WaitForQueueToSettleAsync();

        var messageCount = await fixture.CountInboxMessagesAsync(TransportTestData.DuplicatedMessageId);

        messageCount.Should().Be(1);
    }

    [Fact]
    public async Task RebusDelivery_WithoutActorHeader_FailsFastToErrorQueue()
    {
        await fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.PingPayload,
            TransportTestData.HeaderlessMessageId,
            new Dictionary<string, string>());

        var errorDetails = await RabbitMqQueueReader.ReadHeaderAsync(
            fixture.BrokerConnectionString,
            TransportTestData.ErrorQueueName,
            TransportTestData.HeaderlessMessageId,
            TransportTestData.ErrorDetailsHeader,
            TransportTestData.WaitTimeout);

        var messageCount = await fixture.CountInboxMessagesAsync(TransportTestData.HeaderlessMessageId);

        errorDetails.Should().StartWith(TransportTestData.FailedOnceText);
        messageCount.Should().Be(0);
    }

    [Fact]
    public async Task RebusDelivery_ToErrorQueue_LogsError()
    {
        await fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.PingPayload,
            TransportTestData.LoggedMessageId,
            new Dictionary<string, string>());

        var errors = await fixture.WaitForErrorLogsAsync(TransportTestData.LoggedMessageId);

        errors.Should().ContainSingle();
    }

    [Fact]
    public async Task RebusDelivery_WithEmptyMessageId_MovesMessageToErrorQueue()
    {
        await fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.PingPayload,
            Guid.Empty,
            TransportTestData.ActorHeaders());

        var isInErrorQueue = await RabbitMqQueueReader.ContainsMessageAsync(
            fixture.BrokerConnectionString,
            TransportTestData.ErrorQueueName,
            Guid.Empty,
            TransportTestData.WaitTimeout);

        var messageCount = await fixture.CountInboxMessagesAsync(Guid.Empty);

        isInErrorQueue.Should().BeTrue();
        messageCount.Should().Be(0);
    }
}
