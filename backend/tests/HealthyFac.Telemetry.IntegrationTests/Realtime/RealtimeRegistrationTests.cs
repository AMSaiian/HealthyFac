using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.StatusDefinitions;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.Realtime;

[Collection(TelemetryDefinitionsCollection.Name)]
[Feature(TestFeatures.Realtime)]
public sealed class RealtimeRegistrationTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public void Notifier_WithAHintPushed_ReachesTheRealtimeQueue()
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var notifier = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();
        notifier.AddHint(Guid.NewGuid(), new RealtimeHint { Topic = RealtimeTopic.Machines });

        notifier.Push();

        _fixture.RealtimeQueue.Items.Should().ContainSingle();
    }
}
