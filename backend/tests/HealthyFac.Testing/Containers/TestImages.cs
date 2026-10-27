namespace HealthyFac.Testing.Containers;

/// <summary>
/// Pins the container images that integration tests start. Every test project uses these constants, so a version
/// bump is one edit. Keep each tag equal to the image of the same service in <c>docker-compose.yml</c>, so tests
/// run against the version that development uses. Never rely on the default image of a Testcontainers module,
/// because it changes with library upgrades.
/// </summary>
public static class TestImages
{
    public const string Postgres = "postgres:18.6-alpine";
    public const string Redis = "redis:8.10.1-alpine";
    public const string RabbitMq = "rabbitmq:4.3.5-management-alpine";
    public const string Mailpit = "axllent/mailpit:v1.31.5";
}
