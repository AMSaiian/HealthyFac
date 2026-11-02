namespace HealthyFac.Auth.IntegrationTests.Identities.Data;

/// <summary>
/// Shares one <see cref="ProvisioningFixture"/>, and with it one Keycloak, Mailpit, Redis, PostgreSQL, and RabbitMQ
/// container, across the test classes that need Keycloak. Every test creates its own employee ID and email, so the
/// tests do not need separate containers. The classes of the collection run one after another.
/// </summary>
[CollectionDefinition(Name)]
public sealed class KeycloakCollection : ICollectionFixture<ProvisioningFixture>
{
    public const string Name = "Keycloak";
}
