namespace HealthyFac.Identity.IntegrationTests.Realm.Data;

/// <summary>
/// Shares one <see cref="KeycloakRealmCollectionFixture"/>, and with it one Keycloak and one Redis container, across
/// the test classes that only add clients, scopes, and roles to the imported realm. The classes run one after another,
/// so a test that disables the supervisor for a moment never overlaps another test. A class that changes the realm
/// itself gets its own collection.
/// </summary>
[CollectionDefinition(Name)]
public sealed class KeycloakRealmCollection : ICollectionFixture<KeycloakRealmCollectionFixture>
{
    public const string Name = "KeycloakRealm";
}
