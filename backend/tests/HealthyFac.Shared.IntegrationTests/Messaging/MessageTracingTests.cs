using System.Diagnostics;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Infrastructure.Messaging.Processing;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;
using HealthyFac.Testing.Tracing;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class MessageTracingTests(TransportFixture fixture) : IClassFixture<TransportFixture>
{
    [Fact]
    public async Task Process_WithoutTraceParent_StartsNewTrace()
    {
        using var capture = new SpanCapture(ObservabilityConstants.Sources.Messaging);
        await fixture.AddPendingInboxRowAsync(
            TracingTestData.NoParentRowId,
            InboxProcessorTestData.RecordedMessageType,
            TracingTestData.NoParentText,
            InboxProcessorTestData.UserActorId,
            InboxProcessorTestData.UserCompanyId,
            InboxTestData.Now);
        var processor = fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var spans = await capture.WaitForSpansAsync(
            span => span.IsOperation(ObservabilityConstants.Messaging.Operations.Process)
                && span.ReadMessageId() == TracingTestData.NoParentRowId.ToString(),
            expectedCount: 1);

        spans.Should().ContainSingle().Which.ParentId.Should().BeNull();
    }

    [Fact]
    public async Task Process_WhenHandlerThrows_MarksSpanFailed()
    {
        using var capture = new SpanCapture(ObservabilityConstants.Sources.Messaging);
        await fixture.AddPendingInboxRowAsync(
            TracingTestData.FailingRowId,
            InboxProcessorTestData.FailingMessageType,
            InboxProcessorTestData.FailingText,
            InboxProcessorTestData.UserActorId,
            InboxProcessorTestData.UserCompanyId,
            InboxTestData.Now);
        var processor = fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var spans = await capture.WaitForSpansAsync(
            span => span.IsOperation(ObservabilityConstants.Messaging.Operations.Process)
                && span.ReadMessageId() == TracingTestData.FailingRowId.ToString(),
            expectedCount: 1);

        spans.Should().ContainSingle().Which.Status.Should().Be(ActivityStatusCode.Error);
    }
}
