using System.Reflection;
using StackExchange.Redis;

namespace HealthyFac.Shared.UnitTests.ServiceTokens.Data;

/// <summary>
/// Plays a Redis connection that counts every call and answers each one with a Redis failure, so a test can tell
/// whether a call reached Redis at all. Create it with <see cref="Create"/>.
/// </summary>
public class CallCountingConnection : DispatchProxy
{
    private const string SimulatedFailure = "Redis is down.";

    private int _callCount;

    public int CallCount => Volatile.Read(ref _callCount);

    /// <summary>
    /// Creates the fake connection.
    /// </summary>
    /// <returns>The connection and the proxy that counts its calls.</returns>
    public static (IConnectionMultiplexer Connection, CallCountingConnection Counter) Create()
    {
        var connection = DispatchProxy.Create<IConnectionMultiplexer, CallCountingConnection>();

        return (connection, (CallCountingConnection)(object)connection);
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        Interlocked.Increment(ref _callCount);

        throw new RedisException(SimulatedFailure);
    }
}
