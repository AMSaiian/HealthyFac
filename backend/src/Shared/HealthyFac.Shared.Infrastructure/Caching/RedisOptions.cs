namespace HealthyFac.Shared.Infrastructure.Caching;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public required string ConnectionString { get; init; }

    public int ConnectTimeoutMilliseconds { get; init; } = 1000;

    public int OperationTimeoutMilliseconds { get; init; } = 500;
}
