using HealthyFac.Shared.Infrastructure.Messaging.Processing;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

/// <summary>
/// The transport fixture with both processors running as hosted services, so a message travels from an outbox row
/// through the broker and the inbox to its handler without a manual step.
/// </summary>
public sealed class PipelineFixture : TransportFixture
{
    protected override void ConfigureServices(IServiceCollection services) => services.AddMessageProcessors();
}
