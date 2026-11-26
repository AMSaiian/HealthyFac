using HealthyFac.Shared.Infrastructure.Messaging.Receiving;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class InboxRealtimeRetryTests(InboxRealtimeRetryFixture fixture)
    : IClassFixture<InboxRealtimeRetryFixture>
{
    private readonly InboxRealtimeRetryFixture _fixture = fixture;

    [Fact]
    public async Task InboxProcessor_WhenTheExecutionStrategyRepeatsTheOperation_EnqueuesTheEventOnce()
    {
        await _fixture.AddPendingInboxRowAsync(
            InboxRealtimeTestData.RepeatedRowId,
            InboxRealtimeTestData.MessageType,
            InboxRealtimeTestData.RepeatedText,
            InboxProcessorTestData.UserActorId,
            InboxProcessorTestData.UserCompanyId,
            InboxTestData.Now);

        var processor = _fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var row = await _fixture.ReadInboxRowAsync(InboxRealtimeTestData.RepeatedRowId);
        var calls = _fixture.Recorder.CallsFor(InboxRealtimeTestData.RepeatedText);

        row.ProcessedOn.Should().Be(InboxTestData.Now);
        _fixture.InjectedFailureCount.Should().BeGreaterThanOrEqualTo(expected: 1);
        calls.Should().HaveCount(expected: 2);
        _fixture.RealtimeQueue.Items.Should().ContainSingle();

        _fixture.InboxObserver.Calls.Count(call => call == InboxRealtimeTestData.StartingCall)
            .Should()
            .BeGreaterThanOrEqualTo(expected: 2);

        _fixture.InboxObserver.Calls.Count(call => call == InboxRealtimeTestData.CommittedCall)
            .Should()
            .Be(expected: 1);
    }
}
