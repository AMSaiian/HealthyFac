namespace HealthyFac.Testing.Realtime;

public static class RealtimeTestSettings
{
    /// <summary>
    /// A Redis address that nothing listens on. A host that registers the push needs a Redis setting, and a test
    /// that never pushes needs no Redis. The multiplexer connects without aborting.
    /// </summary>
    public const string UnreachableRedis = "127.0.0.1:1";
}
