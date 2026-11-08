namespace HealthyFac.Identity.IntegrationTests.Realm.Data;

/// <summary>
/// Gives the shared permissions claim tests their own Keycloak, because they change the realm and no other class may
/// see the change.
/// </summary>
[CollectionDefinition(Name)]
public sealed class SharedPermissionsClaimCollection : ICollectionFixture<SharedPermissionsClaimFixture>
{
    public const string Name = "SharedPermissionsClaim";
}
