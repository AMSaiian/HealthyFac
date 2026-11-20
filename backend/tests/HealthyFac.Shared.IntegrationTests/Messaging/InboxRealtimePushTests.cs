using HealthyFac.Shared.Infrastructure.Messaging.Receiving;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

[Collection(MessagingTransportCollection.Name)]
public sealed class InboxRealtimePushTests(TransportCollectionFixture fixture)
{
    private readonly TransportCollectionFixture _fixture = fixture;

    [Fact]
    public async Task InboxProcessor_WithHandledRow_EnqueuesTheEventOnceAfterTheCommit()
    {
        await _fixture.AddPendingInboxRowAsync(
            InboxRealtimeTestData.HandledRowId,
            InboxRealtimeTestData.MessageType,
            InboxRealtimeTestData.HandledText,
            InboxProcessorTestData.UserActorId,
            InboxProcessorTestData.UserCompanyId,
            InboxTestData.Now);
        var processor = _fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var row = await _fixture.ReadInboxRowAsync(InboxRealtimeTestData.HandledRowId);
        var push = _fixture.RealtimeQueue.Items.Should().ContainSingle().Subject.Push;

        row.ProcessedOn.Should().Be(InboxTestData.Now);
        push.Notification!.Kind.Value.Should().Be(expected: "workOrderBlocked");
    }

    [Fact]
    public async Task InboxProcessor_WhenTheHandlerAddsThenThrows_EnqueuesNothingUntilTheRetrySucceeds()
    {
        await _fixture.AddPendingInboxRowAsync(
            InboxRealtimeTestData.FailsOnceRowId,
            InboxRealtimeTestData.MessageType,
            InboxRealtimeTestData.FailsOnceText,
            InboxProcessorTestData.UserActorId,
            InboxProcessorTestData.UserCompanyId,
            InboxTestData.Now);
        var processor = _fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);
        var countAfterFailure = _fixture.RealtimeQueue.Items.Count;
        await _fixture.MakeInboxRowDueAsync(InboxRealtimeTestData.FailsOnceRowId);
        await processor.RunAsync(CancellationToken.None);

        var row = await _fixture.ReadInboxRowAsync(InboxRealtimeTestData.FailsOnceRowId);

        countAfterFailure.Should().Be(expected: 0);
        row.ProcessedOn.Should().Be(InboxTestData.Now);
        _fixture.RealtimeQueue.Items.Should().ContainSingle();
    }
}
