using HealthyFac.Shared.Infrastructure.Messaging.Processing;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class OutboxProcessorBrokerOutageTests(TransportFixture fixture) : IClassFixture<TransportFixture>
{
    [Fact]
    public async Task OutboxProcessor_WhenBrokerRejects_SchedulesRetry()
    {
        await fixture.AddPendingOutboxRowsAsync(
            OutboxProcessorTestData.OutageMessageType,
            OutboxProcessorTestData.SingleRowCount);
        await fixture.StopBrokerAsync();
        var processor = fixture.Create<OutboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var rows = await fixture.ReadOutboxRowsAsync(OutboxProcessorTestData.OutageMessageType);

        var row = rows.Should().ContainSingle().Which;
        row.ProcessedOn.Should().BeNull();
        row.AttemptCount.Should().Be(OutboxProcessorTestData.FailedAttemptCount);
        row.Error.Should().NotBeNullOrWhiteSpace();
        row.NextAttemptOn.Should().BeAfter(InboxTestData.Now);
    }

    [Fact]
    public async Task OutboxProcessor_WhenBrokerRejects_StopsRunAfterFirstFailure()
    {
        await fixture.AddPendingOutboxRowsAsync(
            OutboxProcessorTestData.OutageBatchMessageType,
            OutboxProcessorTestData.OutageBatchRowCount);
        await fixture.StopBrokerAsync();
        var processor = fixture.Create<OutboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var rows = await fixture.ReadOutboxRowsAsync(OutboxProcessorTestData.OutageBatchMessageType);

        rows.Count(row => row.AttemptCount == OutboxProcessorTestData.FailedAttemptCount).Should().Be(1);
        rows.Count(row => row.AttemptCount == 0).Should().Be(OutboxProcessorTestData.OutageBatchRowCount - 1);
    }
}
