using HealthyFac.Shared.Infrastructure.Messaging.Sending;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class OutboxProcessorBrokerOutageTests(TransportFixture fixture) : IClassFixture<TransportFixture>
{
    private readonly TransportFixture _fixture = fixture;

    [Fact]
    public async Task OutboxProcessor_WhenBrokerRejects_SchedulesRetryAndStopsRunAfterFirstFailure()
    {
        await _fixture.AddPendingOutboxRowsAsync(
            OutboxProcessorTestData.OutageBatchMessageType,
            OutboxProcessorTestData.OutageBatchRowCount);
        await _fixture.StopBrokerAsync();
        var processor = _fixture.Create<OutboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var rows = await _fixture.ReadOutboxRowsAsync(OutboxProcessorTestData.OutageBatchMessageType);

        var failedRow = rows.Should()
            .ContainSingle(row => row.AttemptCount == OutboxProcessorTestData.FailedAttemptCount)
            .Which;
        failedRow.ProcessedOn.Should().BeNull();
        failedRow.Error.Should().NotBeNullOrWhiteSpace();
        failedRow.NextAttemptOn.Should().BeAfter(InboxTestData.Now);

        rows.Count(row => row.AttemptCount == 0).Should().Be(OutboxProcessorTestData.OutageBatchRowCount - 1);
    }
}
