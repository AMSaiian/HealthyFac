using HealthyFac.Auth.Application.Identities;
using StackExchange.Redis;

namespace HealthyFac.Auth.IntegrationTests.Identities.Data;

/// <summary>
/// Plays a Redis outage. Every call throws a <see cref="RedisException"/>, which <c>RedisSessionRevoker</c> lets
/// through.
/// </summary>
public sealed class FailingSessionRevoker : ISessionRevoker
{
    public int CallCount { get; private set; }

    public Task RevokeAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
    {
        CallCount++;

        throw new RedisException(ProvisioningTestData.RedisUnavailableMessage);
    }
}
