using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Realtime;

[Collection(CoreEmployeesCollection.Name)]
[Feature(TestFeatures.Realtime)]
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
