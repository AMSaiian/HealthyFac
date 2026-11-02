using System.Reflection;
using StackExchange.Redis;

namespace HealthyFac.Shared.UnitTests.Caching.Data;

/// <summary>
/// Plays a Redis connection that is back: its database answers every <c>PING</c>. Every other call fails, so a test
/// notices a call that it did not expect. Create it with <see cref="Create"/>.
/// </summary>
public class PingAnsweringConnection : DispatchProxy
{
    private const string UnexpectedCall = "The test expects only a ping.";

    /// <summary>
    /// Creates the fake connection.
    /// </summary>
    /// <returns>The connection.</returns>
    public static IConnectionMultiplexer Create() =>
        DispatchProxy.Create<IConnectionMultiplexer, PingAnsweringConnection>();

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
        targetMethod?.Name switch
        {
            nameof(IConnectionMultiplexer.GetDatabase) => DispatchProxy.Create<IDatabase, PingAnsweringDatabase>(),
            _ => throw new InvalidOperationException(UnexpectedCall),
        };

    /// <summary>
    /// Plays the database of <see cref="PingAnsweringConnection"/>.
    /// </summary>
    public class PingAnsweringDatabase : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name switch
            {
                nameof(IDatabase.PingAsync) => Task.FromResult(TimeSpan.Zero),
                _ => throw new InvalidOperationException(UnexpectedCall),
            };
    }
}
