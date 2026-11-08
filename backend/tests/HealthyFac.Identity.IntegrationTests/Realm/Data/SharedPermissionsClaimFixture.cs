using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Resetting;

namespace HealthyFac.Identity.IntegrationTests.Realm.Data;

/// <summary>
/// A Keycloak container for the merge test only. The test changes the realm, so the other realm tests never share
/// this container.
/// </summary>
public sealed class SharedPermissionsClaimFixture : IAsyncLifetime, IResetBeforeEachTest
{
    public KeycloakTestServer Server { get; } = new();

    public async ValueTask InitializeAsync() => await Server.StartAsync(RealmTestData.Environment);

    public async ValueTask ResetAsync() => await Server.DeleteTestIdentitiesAsync();

    public async ValueTask DisposeAsync() => await Server.DisposeAsync();
}
