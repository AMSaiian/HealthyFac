namespace HealthyFac.Shared.Infrastructure.Caching;

/// <summary>
/// Defines the validator messages for the Redis configuration.
/// </summary>
public static class RedisValidationMessages
{
    public const string ConnectionStringRequired = "The Redis connection string is required.";

    public static readonly string ConnectTimeoutOutOfRange =
        $"The Redis connect timeout must be from {RedisConstants.MinTimeoutMilliseconds} "
        + $"to {RedisConstants.MaxTimeoutMilliseconds} milliseconds.";

    public static readonly string OperationTimeoutOutOfRange =
        $"The Redis operation timeout must be from {RedisConstants.MinTimeoutMilliseconds} "
        + $"to {RedisConstants.MaxTimeoutMilliseconds} milliseconds.";
}
