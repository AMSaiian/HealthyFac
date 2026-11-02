using System.Diagnostics.Metrics;
using HealthyFac.Shared.Infrastructure.Messaging.Processing;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Constants = HealthyFac.Shared.Application.Observability.ObservabilityConstants;
using Instruments = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Messaging.Instruments;
using Processors = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Messaging.Processors;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

/// <summary>
/// The two tests share one database. The second test marks every pending row processed, so the first test sees only
/// its own rows in either order.
/// </summary>
public sealed class MessageBacklogSamplerTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture = fixture;

    [Fact]
    public async Task Sample_WithPendingRows_ReportsCountAndOldestAge()
    {
        await _fixture.SeedBacklogAsync();
        using var pending = Collect<long>(Instruments.Pending);
        using var age = Collect<double>(Instruments.OldestPendingAge);
        var sampler = CreateSampler();

        await sampler.SampleAsync(CancellationToken.None);

        var outboxPending = pending.ObserveForProcessor(Processors.Outbox);
        var inboxPending = pending.ObserveForProcessor(Processors.Inbox);
        var outboxAge = age.ObserveForProcessor(Processors.Outbox);
        var inboxAge = age.ObserveForProcessor(Processors.Inbox);

        outboxPending.Should().Equal(MessagingMetricsTestData.PendingOutboxCount);
        inboxPending.Should().Equal(MessagingMetricsTestData.PendingInboxCount);
        outboxAge.Should().Equal(MessagingMetricsTestData.OutboxAgeSeconds);
        inboxAge.Should().Equal(MessagingMetricsTestData.InboxAgeSeconds);
    }

    [Fact]
    public async Task Sample_AfterProcessing_ReportsZero()
    {
        await _fixture.SeedBacklogAsync();
        await _fixture.MarkBacklogProcessedAsync();
        using var pending = Collect<long>(Instruments.Pending);
        using var age = Collect<double>(Instruments.OldestPendingAge);
        var sampler = CreateSampler();

        await sampler.SampleAsync(CancellationToken.None);

        var outboxPending = pending.ObserveForProcessor(Processors.Outbox);
        var inboxPending = pending.ObserveForProcessor(Processors.Inbox);
        var outboxAge = age.ObserveForProcessor(Processors.Outbox);
        var inboxAge = age.ObserveForProcessor(Processors.Inbox);

        outboxPending.Should().Equal(MessagingMetricsTestData.NoMessages);
        inboxPending.Should().Equal(MessagingMetricsTestData.NoMessages);
        outboxAge.Should().Equal(MessagingMetricsTestData.NoAge);
        inboxAge.Should().Equal(MessagingMetricsTestData.NoAge);
    }

    private MetricCollector<T> Collect<T>(string instrument)
        where T : struct =>
        new(_fixture.Services.GetRequiredService<IMeterFactory>(), Constants.Meters.Messaging, instrument);

    private MessageBacklogSampler CreateSampler() =>
        ActivatorUtilities.CreateInstance<MessageBacklogSampler>(
            _fixture.Services,
            NullLogger<MessageBacklogSampler>.Instance);
}
