using System.Collections.Frozen;
using System.Diagnostics;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;
using HealthyFac.Testing.Tracing;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class EndToEndTests(PipelineFixture fixture) : IClassFixture<PipelineFixture>
{
    private readonly PipelineFixture _fixture = fixture;

    [Fact]
    public async Task Pipeline_WithTraceParent_LinksSendReceiveAndProcess()
    {
        using var capture = new SpanCapture(ObservabilityConstants.Sources.Messaging);
        using var parent = new Activity(TracingTestData.ParentName).Start();
        using (var scope = _fixture.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IOutbox>()
                .Add(new TestRecorded { Text = TracingTestData.LinkedText });
            await scope.ServiceProvider.GetRequiredService<ITestServiceDbContext>()
                .SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var spans = await capture.WaitForSpansAsync(
            span => span.TraceId == parent.TraceId,
            TracingTestData.ExpectedSpanCount);

        var send = spans.Single(span => span.IsOperation(ObservabilityConstants.Messaging.Operations.Send));
        var receive = spans.Single(span => span.IsOperation(ObservabilityConstants.Messaging.Operations.Receive));
        var process = spans.Single(span => span.IsOperation(ObservabilityConstants.Messaging.Operations.Process));

        send.ParentId.Should().Be(parent.Id);
        receive.ParentId.Should().Be(send.Id);
        process.ParentId.Should().Be(receive.Id);
    }

    [Fact]
    public async Task EndToEnd_OutboxToInbox_HandlesMessageOnce()
    {
        using var scope = _fixture.CreateScope(
            new ActorContext
            {
                Kind = ActorKind.User,
                UserId = InboxProcessorTestData.UserActorId,
                CompanyId = InboxProcessorTestData.UserCompanyId,
                DepartmentIds = FrozenSet<Guid>.Empty,
            });
        scope.ServiceProvider.GetRequiredService<IOutbox>()
            .Add(new TestRecorded { Text = InboxProcessorTestData.EndToEndText });
        await scope.ServiceProvider.GetRequiredService<ITestServiceDbContext>()
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        var calls = await _fixture.WaitForHandledCallsAsync(InboxProcessorTestData.EndToEndText);

        var actor = calls.Should().ContainSingle().Which.Actor!;
        actor.UserId.Should().Be(InboxProcessorTestData.UserActorId);
        actor.CompanyId.Should().Be(InboxProcessorTestData.UserCompanyId);
    }
}
