using System.Text.Json.Nodes;
using HealthyFac.Shared.Infrastructure.Messaging.Processing;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class TransportTests(TransportFixture fixture) : IClassFixture<TransportFixture>
{
    private readonly TransportFixture _fixture = fixture;

    [Fact]
    public async Task RebusDelivery_WithActorHeaders_StoresInboxRowWithActor()
    {
        await _fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.PingPayload,
            TransportTestData.DeliveredMessageId,
            TransportTestData.ActorHeaders());

        var stored = await _fixture.WaitForInboxMessageAsync(TransportTestData.DeliveredMessageId);

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
        await _fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.PingPayload,
            TransportTestData.DuplicatedMessageId,
            TransportTestData.ActorHeaders());
        await _fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.PingPayload,
            TransportTestData.DuplicatedMessageId,
            TransportTestData.ActorHeaders());
        await _fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.PingPayload,
            TransportTestData.SentinelMessageId,
            TransportTestData.ActorHeaders());

        await _fixture.WaitForInboxMessageAsync(TransportTestData.SentinelMessageId);
        await _fixture.WaitForQueueToSettleAsync();

        var messageCount = await _fixture.CountInboxMessagesAsync(TransportTestData.DuplicatedMessageId);

        messageCount.Should().Be(1);
    }

    [Fact]
    public async Task RebusDelivery_WithoutActorHeader_FailsFastToErrorQueue()
    {
        await _fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.PingPayload,
            TransportTestData.HeaderlessMessageId,
            new Dictionary<string, string>());

        var errorDetails = await RabbitMqQueueReader.ReadHeaderAsync(
            _fixture.BrokerConnectionString,
            TransportTestData.ErrorQueueName,
            TransportTestData.HeaderlessMessageId,
            TransportTestData.ErrorDetailsHeader,
            TransportTestData.WaitTimeout);

        var messageCount = await _fixture.CountInboxMessagesAsync(TransportTestData.HeaderlessMessageId);

        errorDetails.Should().StartWith(TransportTestData.FailedOnceText);
        messageCount.Should().Be(0);
    }

    [Fact]
    public async Task RebusDelivery_InvalidPayloadTwice_StoresOneFailedRow()
    {
        await _fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.BlankTextPayload,
            TransportTestData.InvalidPayloadMessageId,
            TransportTestData.ActorHeaders());
        await _fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.BlankTextPayload,
            TransportTestData.InvalidPayloadMessageId,
            TransportTestData.ActorHeaders());
        await _fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.PingPayload,
            TransportTestData.InvalidSentinelMessageId,
            TransportTestData.ActorHeaders());
        await _fixture.WaitForInboxMessageAsync(TransportTestData.InvalidSentinelMessageId);
        await _fixture.WaitForQueueToSettleAsync();
        var processor = _fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var messageCount = await _fixture.CountInboxMessagesAsync(TransportTestData.InvalidPayloadMessageId);
        var row = await _fixture.ReadInboxRowAsync(TransportTestData.InvalidPayloadMessageId);

        messageCount.Should().Be(1);
        row.FailedOn.Should().NotBeNull();
        row.AttemptCount.Should().Be(InboxProcessorTestData.FailedAttemptCount);
    }

    [Fact]
    public async Task RebusDelivery_WithPayloadNotJson_FailsFastToErrorQueue()
    {
        await _fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.NotJsonPayload,
            TransportTestData.NotJsonMessageId,
            TransportTestData.ActorHeaders());

        var errorDetails = await RabbitMqQueueReader.ReadHeaderAsync(
            _fixture.BrokerConnectionString,
            TransportTestData.ErrorQueueName,
            TransportTestData.NotJsonMessageId,
            TransportTestData.ErrorDetailsHeader,
            TransportTestData.WaitTimeout);

        var messageCount = await _fixture.CountInboxMessagesAsync(TransportTestData.NotJsonMessageId);

        errorDetails.Should().StartWith(TransportTestData.FailedOnceText);
        messageCount.Should().Be(0);
    }

    [Fact]
    public async Task RebusDelivery_WithNullCharacterEscape_FailsFastToErrorQueue()
    {
        await _fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.NullCharacterEscapePayload,
            TransportTestData.NullCharacterMessageId,
            TransportTestData.ActorHeaders());

        var errorDetails = await RabbitMqQueueReader.ReadHeaderAsync(
            _fixture.BrokerConnectionString,
            TransportTestData.ErrorQueueName,
            TransportTestData.NullCharacterMessageId,
            TransportTestData.ErrorDetailsHeader,
            TransportTestData.WaitTimeout);

        var messageCount = await _fixture.CountInboxMessagesAsync(TransportTestData.NullCharacterMessageId);

        errorDetails.Should().StartWith(TransportTestData.FailedOnceText);
        messageCount.Should().Be(0);
    }

    [Fact]
    public async Task RebusDelivery_ToErrorQueue_LogsError()
    {
        await _fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.PingPayload,
            TransportTestData.LoggedMessageId,
            new Dictionary<string, string>());

        var errors = await _fixture.WaitForErrorLogsAsync(TransportTestData.LoggedMessageId);

        errors.Should().ContainSingle();
    }

    [Fact]
    public async Task RebusDelivery_WithEmptyMessageId_MovesMessageToErrorQueue()
    {
        await _fixture.PublishAsync(
            TransportTestData.PingMessageType,
            TransportTestData.PingPayload,
            Guid.Empty,
            TransportTestData.ActorHeaders());

        var isInErrorQueue = await RabbitMqQueueReader.ContainsMessageAsync(
            _fixture.BrokerConnectionString,
            TransportTestData.ErrorQueueName,
            Guid.Empty,
            TransportTestData.WaitTimeout);

        var messageCount = await _fixture.CountInboxMessagesAsync(Guid.Empty);

        isInErrorQueue.Should().BeTrue();
        messageCount.Should().Be(0);
    }
}
