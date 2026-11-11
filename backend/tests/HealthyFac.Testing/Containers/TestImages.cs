namespace HealthyFac.Testing.Containers;

/// <summary>
/// Pins the container images that integration tests start. Every test project uses these constants, so a version
/// bump is one edit. Keep each tag equal to the image of the same service in <c>docker-compose.yml</c>, so tests
/// run against the version that development uses. Never rely on the default image of a Testcontainers module,
/// because it changes with library upgrades.
/// </summary>
public static class TestImages
{
    public const string Postgres = "pgvector/pgvector:0.8.6-pg18-trixie";
    public const string Redis = "redis:8.10.1-alpine";
    public const string RabbitMq = "rabbitmq:4.3.5-management-alpine";
    public const string Mosquitto = "eclipse-mosquitto:2.1.2-alpine";
    public const string Mailpit = "axllent/mailpit:v1.31.5";
    public const string Azurite = "mcr.microsoft.com/azure-storage/azurite:3.37.0";
    public const string Keycloak = "quay.io/keycloak/keycloak:26.8.0";
    public const string KeycloakConfigCli = "adorsys/keycloak-config-cli:6.5.1-26";
    public const string Docling = "ghcr.io/docling-project/docling-serve-cu130:v1.38.2";
}
