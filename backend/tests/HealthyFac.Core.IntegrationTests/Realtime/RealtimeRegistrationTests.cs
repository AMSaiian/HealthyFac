using HealthyFac.Core.IntegrationTests.Database;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.Realtime;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Realtime;

[Collection(CoreSeedingCollection.Name)]
public sealed class RealtimeRegistrationTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

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
