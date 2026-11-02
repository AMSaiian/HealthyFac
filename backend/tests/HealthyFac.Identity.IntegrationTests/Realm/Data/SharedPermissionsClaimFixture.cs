using HealthyFac.Testing.Containers;

namespace HealthyFac.Identity.IntegrationTests.Realm.Data;

/// <summary>
/// A Keycloak container for the merge test only. The test changes the realm, so the other realm tests never share
/// this container.
/// </summary>
public sealed class SharedPermissionsClaimFixture : IAsyncLifetime
{
    public KeycloakTestServer Server { get; } = new();

    public Task InitializeAsync() => Server.StartAsync(RealmTestData.Environment);

    public async Task DisposeAsync() => await Server.DisposeAsync();
}
