using System.Collections.Concurrent;
using System.Text;
using HealthyFac.Telemetry.IntegrationTests.Intake.Data;
using MQTTnet;
using MQTTnet.Protocol;

namespace HealthyFac.Telemetry.IntegrationTests.Intake;

public sealed class MqttClientBehaviorTests(MosquittoFixture fixture) : IClassFixture<MosquittoFixture>
{
    private readonly MosquittoFixture _fixture = fixture;

    [Fact]
    public async Task SharedSubscription_WithTwoClients_DeliversEachMessageExactlyOnceInTotal()
    {
        var sharedFilter = NewSharedFilter();
        var received = new ConcurrentBag<string>();
        await using var first = new ReceivingClient(await _fixture.ConnectTelemetryAsync(), received);
        await using var second = new ReceivingClient(await _fixture.ConnectTelemetryAsync(), received);
        await first.SubscribeAsync(sharedFilter);
        await second.SubscribeAsync(sharedFilter);
        using var sender = await _fixture.ConnectSenderAsync();

        for (var index = 0; index < IntakeTestData.MessageCount; index++)
        {
            await MosquittoFixture.PublishAsync(
                sender,
                string.Format(IntakeTestData.GatewayTopicFormat, $"gateway{index}"),
                string.Format(IntakeTestData.PayloadFormat, index));
        }

        await ReceivingClient.WaitUntilAsync(() => received.Count >= IntakeTestData.MessageCount);
        await Task.Delay(IntakeTestData.QuietPeriod);

        received.Should().HaveCount(IntakeTestData.MessageCount);
        received.Distinct().Should().HaveCount(IntakeTestData.MessageCount);
        first.Count.Should().BeGreaterThan(expected: 0).And.BeLessThan(IntakeTestData.MessageCount);
        second.Count.Should().BeGreaterThan(expected: 0).And.BeLessThan(IntakeTestData.MessageCount);
    }

    [Fact]
    public async Task UnacknowledgedMessage_AfterReconnectWithSameSession_IsDeliveredAgain()
    {
        const string clientId = "redelivery-client";
        var sharedFilter = NewSharedFilter();
        var first = new ConcurrentBag<string>();
        var client = await _fixture.ConnectTelemetryAsync(
            clientId, isCleanStart: false, IntakeTestData.SessionExpirySeconds);
        await using (var receiving = new ReceivingClient(client, first, isAcknowledging: false))
        {
            await receiving.SubscribeAsync(sharedFilter);
            using var sender = await _fixture.ConnectSenderAsync();
            await MosquittoFixture.PublishAsync(
                sender,
                string.Format(IntakeTestData.GatewayTopicFormat, arg0: "gateway0"),
                string.Format(IntakeTestData.PayloadFormat, arg0: 0));
            await ReceivingClient.WaitUntilAsync(() => first.Count >= 1);
        }

        var second = new ConcurrentBag<string>();
        var reconnected = await _fixture.ConnectTelemetryAsync(
            clientId, isCleanStart: false, IntakeTestData.SessionExpirySeconds);
        await using var receivingAgain = new ReceivingClient(reconnected, second);
        await ReceivingClient.WaitUntilAsync(() => second.Count >= 1);

        first.Should().ContainSingle();
        second.Should().ContainSingle().Which.Should().Be(first.Single());
    }

    [Fact]
    public async Task AcknowledgedMessage_AfterReconnectWithSameSession_IsNotDeliveredAgain()
    {
        const string clientId = "acknowledged-client";
        var sharedFilter = NewSharedFilter();
        var first = new ConcurrentBag<string>();
        var client = await _fixture.ConnectTelemetryAsync(
            clientId, isCleanStart: false, IntakeTestData.SessionExpirySeconds);
        await using (var receiving = new ReceivingClient(client, first))
        {
            await receiving.SubscribeAsync(sharedFilter);
            using var sender = await _fixture.ConnectSenderAsync();
            await MosquittoFixture.PublishAsync(
                sender,
                string.Format(IntakeTestData.GatewayTopicFormat, arg0: "gateway0"),
                string.Format(IntakeTestData.PayloadFormat, arg0: 0));
            await ReceivingClient.WaitUntilAsync(() => first.Count >= 1);
            await Task.Delay(IntakeTestData.QuietPeriod);
        }

        var second = new ConcurrentBag<string>();
        var reconnected = await _fixture.ConnectTelemetryAsync(
            clientId, isCleanStart: false, IntakeTestData.SessionExpirySeconds);
        await using var receivingAgain = new ReceivingClient(reconnected, second);
        await Task.Delay(IntakeTestData.QuietPeriod);

        second.Should().BeEmpty();
    }

    [Fact]
    public async Task UnacknowledgedMessage_AfterSessionExpiry_IsDroppedAndNotRedistributed()
    {
        var sharedFilter = NewSharedFilter();
        var deadReceived = new ConcurrentBag<string>();
        var liveReceived = new ConcurrentBag<string>();
        await using var live = new ReceivingClient(await _fixture.ConnectTelemetryAsync(), liveReceived);
        await live.SubscribeAsync(sharedFilter);
        var dead = await _fixture.ConnectTelemetryAsync(
            clientId: "expiring-client", isCleanStart: false, IntakeTestData.ShortSessionExpirySeconds);
        await using (var deadMember = new ReceivingClient(dead, deadReceived, isAcknowledging: false))
        {
            await deadMember.SubscribeAsync(sharedFilter);
            await PublishBatchAsync(offset: 0);
            await ReceivingClient.WaitUntilAsync(() =>
                liveReceived.Count + deadReceived.Count >= IntakeTestData.MessageCount);
        }

        await Task.Delay(IntakeTestData.ExpiryWait);

        deadReceived.Should().NotBeEmpty();
        liveReceived.Distinct().Should().HaveCount(IntakeTestData.MessageCount - deadReceived.Count);
        liveReceived.Intersect(deadReceived).Should().BeEmpty();
    }

    [Fact]
    public async Task NewMessages_AfterSessionExpiry_GoOnlyToLiveMember()
    {
        var sharedFilter = NewSharedFilter();
        var deadReceived = new ConcurrentBag<string>();
        var liveReceived = new ConcurrentBag<string>();
        await using var live = new ReceivingClient(await _fixture.ConnectTelemetryAsync(), liveReceived);
        await live.SubscribeAsync(sharedFilter);
        var dead = await _fixture.ConnectTelemetryAsync(
            clientId: "expired-client", isCleanStart: false, IntakeTestData.ShortSessionExpirySeconds);
        await using (var deadMember = new ReceivingClient(dead, deadReceived, isAcknowledging: false))
        {
            await deadMember.SubscribeAsync(sharedFilter);
        }

        await Task.Delay(IntakeTestData.ExpiryWait);
        await PublishBatchAsync(offset: 0);
        await ReceivingClient.WaitUntilAsync(() => liveReceived.Count >= IntakeTestData.MessageCount);
        await Task.Delay(IntakeTestData.QuietPeriod);

        liveReceived.Distinct().Should().HaveCount(IntakeTestData.MessageCount);
        deadReceived.Should().BeEmpty();
    }

    [Fact]
    public async Task UnacknowledgedMessage_AfterReconnectBeforeExpiry_IsDeliveredAgain()
    {
        const string clientId = "reconnecting-client";
        var sharedFilter = NewSharedFilter();
        var first = new ConcurrentBag<string>();
        var client = await _fixture.ConnectTelemetryAsync(
            clientId, isCleanStart: false, IntakeTestData.ShortSessionExpirySeconds);
        await using (var receiving = new ReceivingClient(client, first, isAcknowledging: false))
        {
            await receiving.SubscribeAsync(sharedFilter);
            await PublishBatchAsync(offset: 0, count: 1);
            await ReceivingClient.WaitUntilAsync(() => first.Count >= 1);
        }

        await Task.Delay(IntakeTestData.ReconnectDelay);
        var second = new ConcurrentBag<string>();
        var reconnected = await _fixture.ConnectTelemetryAsync(
            clientId, isCleanStart: false, IntakeTestData.ShortSessionExpirySeconds);
        await using var receivingAgain = new ReceivingClient(reconnected, second);
        await ReceivingClient.WaitUntilAsync(() => second.Count >= 1);

        second.Should().ContainSingle().Which.Should().Be(first.Single());
    }

    [Fact]
    public async Task NewMessages_WhileMemberIsOfflineAndNotExpired_AreQueuedForThatMember()
    {
        var sharedFilter = NewSharedFilter();
        var offlineReceived = new ConcurrentBag<string>();
        var liveReceived = new ConcurrentBag<string>();
        await using var live = new ReceivingClient(await _fixture.ConnectTelemetryAsync(), liveReceived);
        await live.SubscribeAsync(sharedFilter);
        var offline = await _fixture.ConnectTelemetryAsync(
            clientId: "offline-client", isCleanStart: false, IntakeTestData.SessionExpirySeconds);
        await using (var offlineMember = new ReceivingClient(offline, offlineReceived))
        {
            await offlineMember.SubscribeAsync(sharedFilter);
        }

        await PublishBatchAsync(offset: 0);
        await ReceivingClient.WaitUntilAsync(() => liveReceived.Count >= IntakeTestData.MessageCount);
        await Task.Delay(IntakeTestData.QuietPeriod);
        var liveCountWhileOffline = liveReceived.Count;
        var reconnected = await _fixture.ConnectTelemetryAsync(
            clientId: "offline-client", isCleanStart: false, IntakeTestData.SessionExpirySeconds);
        await using var offlineAgain = new ReceivingClient(reconnected, offlineReceived);
        await ReceivingClient.WaitUntilAsync(() =>
            liveReceived.Count + offlineReceived.Count >= IntakeTestData.MessageCount);

        liveCountWhileOffline.Should().BeLessThan(IntakeTestData.MessageCount);
        offlineReceived.Should().NotBeEmpty();
        liveReceived.Concat(offlineReceived).Distinct().Should().HaveCount(IntakeTestData.MessageCount);
    }

    [Fact]
    public async Task UnsubscribedMember_StillGetsItsAssignedMessagesAndCanAcknowledgeThem()
    {
        var sharedFilter = NewSharedFilter();
        var heldReceived = new ConcurrentBag<string>();
        var liveReceived = new ConcurrentBag<string>();
        await using var live = new ReceivingClient(await _fixture.ConnectTelemetryAsync(), liveReceived);
        await live.SubscribeAsync(sharedFilter);
        var (client, _) = await _fixture.ConnectTelemetryWithResultAsync(
            clientId: "unsubscribing-client", IntakeTestData.SessionExpirySeconds, IntakeTestData.ReceiveWindow);
        await using var held = new ReceivingClient(client, heldReceived, isAcknowledging: false);
        await held.SubscribeAsync(sharedFilter);
        await PublishBatchAsync(offset: 0, IntakeTestData.LargeMessageCount);
        await ReceivingClient.WaitUntilAsync(() => held.Count >= IntakeTestData.ReceiveWindow);
        await Task.Delay(IntakeTestData.QuietPeriod);
        var heldBeforeUnsubscribe = held.Count;

        await held.UnsubscribeAsync(sharedFilter);
        await held.AcknowledgeHeldAndContinueAsync();
        await ReceivingClient.WaitUntilAsync(
            () => liveReceived.Count + heldReceived.Count >= IntakeTestData.LargeMessageCount);
        await Task.Delay(IntakeTestData.QuietPeriod);

        heldBeforeUnsubscribe.Should().Be(IntakeTestData.ReceiveWindow);
        heldReceived.Count.Should().BeGreaterThan(IntakeTestData.ReceiveWindow);
        liveReceived.Concat(heldReceived).Distinct().Should().HaveCount(IntakeTestData.LargeMessageCount);
    }

    [Fact]
    public async Task NewMessages_AfterUnsubscribe_GoOnlyToTheOtherMember()
    {
        var sharedFilter = NewSharedFilter();
        var leavingReceived = new ConcurrentBag<string>();
        var stayingReceived = new ConcurrentBag<string>();
        await using var staying = new ReceivingClient(await _fixture.ConnectTelemetryAsync(), stayingReceived);
        await using var leaving = new ReceivingClient(await _fixture.ConnectTelemetryAsync(), leavingReceived);
        await staying.SubscribeAsync(sharedFilter);
        await leaving.SubscribeAsync(sharedFilter);
        await leaving.UnsubscribeAsync(sharedFilter);

        await PublishBatchAsync(offset: 0);

        await ReceivingClient.WaitUntilAsync(() => stayingReceived.Count >= IntakeTestData.MessageCount);
        await Task.Delay(IntakeTestData.QuietPeriod);

        stayingReceived.Distinct().Should().HaveCount(IntakeTestData.MessageCount);
        leavingReceived.Should().BeEmpty();
    }

    [Fact]
    public async Task Disconnect_WithSessionExpiryZeroInMqttnet_DoesNotDeleteTheSession()
    {
        const string clientId = "deleted-session-client";
        var (client, _) = await _fixture.ConnectTelemetryWithResultAsync(clientId, IntakeTestData.SessionExpirySeconds);
        await client.DisconnectAsync(
            new MqttClientDisconnectOptions
            {
                Reason = MqttClientDisconnectOptionsReason.NormalDisconnection,
                SessionExpiryInterval = 0,
            });
        client.Dispose();

        var (again, result) = await _fixture.ConnectTelemetryWithResultAsync(
            clientId, IntakeTestData.SessionExpirySeconds);
        again.Dispose();

        result.IsSessionPresent.Should().BeTrue();
    }

    [Fact]
    public async Task Disconnect_WithoutChangingSessionExpiry_KeepsTheSession()
    {
        const string clientId = "kept-session-client";
        var (client, _) = await _fixture.ConnectTelemetryWithResultAsync(clientId, IntakeTestData.SessionExpirySeconds);
        await client.DisconnectAsync();
        client.Dispose();

        var (again, result) = await _fixture.ConnectTelemetryWithResultAsync(
            clientId, IntakeTestData.SessionExpirySeconds);
        again.Dispose();

        result.IsSessionPresent.Should().BeTrue();
    }

    [Fact]
    public async Task Reconnect_WithCleanStartAndExpiryZero_DeletesTheOldSession()
    {
        const string clientId = "taken-over-session-client";
        var (client, _) = await _fixture.ConnectTelemetryWithResultAsync(clientId, IntakeTestData.SessionExpirySeconds);
        await client.DisconnectAsync();
        client.Dispose();
        var cleaning = await _fixture.ConnectTelemetryAsync(clientId, isCleanStart: true, sessionExpirySeconds: 0);
        await cleaning.DisconnectAsync();
        cleaning.Dispose();

        var (again, result) = await _fixture.ConnectTelemetryWithResultAsync(
            clientId, IntakeTestData.SessionExpirySeconds);
        again.Dispose();

        result.IsSessionPresent.Should().BeFalse();
    }

    private async Task PublishBatchAsync(int offset, int count = IntakeTestData.MessageCount)
    {
        using var sender = await _fixture.ConnectSenderAsync();

        for (var index = offset; index < offset + count; index++)
        {
            await MosquittoFixture.PublishAsync(
                sender,
                string.Format(IntakeTestData.GatewayTopicFormat, $"gateway{index}"),
                string.Format(IntakeTestData.PayloadFormat, index));
        }
    }

    private static string NewSharedFilter() =>
        string.Format(IntakeTestData.SharedFilterFormat, Guid.NewGuid().ToString(format: "N"));

    [Fact]
    public async Task Connect_WithoutCredentials_IsRefused()
    {
        using var client = _fixture.CreateClient();

        var result = await client.ConnectAsync(_fixture.CreateOptions(user: null, password: null));

        result.ResultCode.Should().Be(MqttClientConnectResultCode.NotAuthorized);
        client.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task Connect_WithWrongPassword_IsRefused()
    {
        using var client = _fixture.CreateClient();

        var result = await client.ConnectAsync(_fixture.CreateOptions(user: "telemetry", password: "wrong-password"));

        result.ResultCode.Should().Be(MqttClientConnectResultCode.NotAuthorized);
        client.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task Subscribe_AsSender_ReceivesNothing()
    {
        var senderReceived = new ConcurrentBag<string>();
        var telemetryReceived = new ConcurrentBag<string>();
        await using var senderListener = new ReceivingClient(await _fixture.ConnectSenderAsync(), senderReceived);
        await using var telemetryListener = new ReceivingClient(
            await _fixture.ConnectTelemetryAsync(), telemetryReceived);
        await senderListener.SubscribeAsync(IntakeTestData.PlainFilter);
        await telemetryListener.SubscribeAsync(IntakeTestData.PlainFilter);
        using var sender = await _fixture.ConnectSenderAsync();

        await MosquittoFixture.PublishAsync(
            sender, string.Format(IntakeTestData.GatewayTopicFormat, arg0: "gateway0"), payload: "{}");

        await ReceivingClient.WaitUntilAsync(() => telemetryReceived.Count >= 1);
        await Task.Delay(IntakeTestData.QuietPeriod);

        telemetryReceived.Should().ContainSingle();
        senderReceived.Should().BeEmpty();
    }

    [Fact]
    public async Task Publish_AsTelemetry_IsRefused()
    {
        using var client = await _fixture.ConnectTelemetryAsync();

        var result = await MosquittoFixture.PublishAsync(
            client, string.Format(IntakeTestData.GatewayTopicFormat, arg0: "gateway0"), payload: "{}");

        result.ReasonCode.Should().Be(MqttClientPublishReasonCode.NotAuthorized);
    }

    [Fact]
    public async Task Publish_AsSenderToOtherTopic_IsRefused()
    {
        using var sender = await _fixture.ConnectSenderAsync();

        var result = await MosquittoFixture.PublishAsync(sender, IntakeTestData.OtherTopic, payload: "{}");

        result.ReasonCode.Should().Be(MqttClientPublishReasonCode.NotAuthorized);
    }

    [Fact]
    public async Task Publish_AsSenderToGatewayTopic_IsAccepted()
    {
        using var sender = await _fixture.ConnectSenderAsync();

        var result = await MosquittoFixture.PublishAsync(
            sender, string.Format(IntakeTestData.GatewayTopicFormat, arg0: "gateway0"), payload: "{}");

        result.ReasonCode.Should()
            .BeOneOf(
                MqttClientPublishReasonCode.Success,
                MqttClientPublishReasonCode.NoMatchingSubscribers);
    }
}

/// <summary>
/// Wraps a client that records the payloads that it receives. It acknowledges a message after it has recorded it,
/// unless the test sets <c>isAcknowledging</c> to <see langword="false"/>.
/// </summary>
internal sealed class ReceivingClient : IAsyncDisposable
{
    private readonly IMqttClient _client;
    private readonly ConcurrentQueue<MqttApplicationMessageReceivedEventArgs> _held = new();
    private int _count;
    private volatile bool _isAcknowledging;

    public ReceivingClient(IMqttClient client, ConcurrentBag<string> received, bool isAcknowledging = true)
    {
        _client = client;
        _isAcknowledging = isAcknowledging;
        _client.ApplicationMessageReceivedAsync += async arguments =>
        {
            arguments.AutoAcknowledge = false;
            received.Add(Encoding.UTF8.GetString(arguments.ApplicationMessage.Payload));
            Interlocked.Increment(ref _count);

            if (_isAcknowledging)
            {
                await arguments.AcknowledgeAsync(CancellationToken.None);
            }
            else
            {
                _held.Enqueue(arguments);
            }
        };
    }

    public int Count => _count;

    /// <summary>
    /// Acknowledges the messages that the client held back and acknowledges every later message at once.
    /// </summary>
    public async Task AcknowledgeHeldAndContinueAsync()
    {
        _isAcknowledging = true;

        while (_held.TryDequeue(out var arguments))
        {
            await arguments.AcknowledgeAsync(CancellationToken.None);
        }
    }

    public Task UnsubscribeAsync(string filter) =>
        _client.UnsubscribeAsync(new MqttClientUnsubscribeOptionsBuilder().WithTopicFilter(filter).Build());

    public Task SubscribeAsync(string filter) =>
        _client.SubscribeAsync(
            new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(filter, MqttQualityOfServiceLevel.AtLeastOnce)
                .Build());

    public static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + IntakeTestData.WaitLimit;

        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds: 50));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client.IsConnected)
        {
            await _client.DisconnectAsync();
        }

        _client.Dispose();
    }
}
