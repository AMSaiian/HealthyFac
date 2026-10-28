namespace HealthyFac.Gateway.IntegrationTests.Hosting;

/// <summary>
/// A request that the <see cref="EchoServer"/> received, as the gateway forwarded it.
/// </summary>
public sealed class EchoedRequest
{
    public required string Method { get; init; }

    public required string Path { get; init; }

    public required string Query { get; init; }

    public required IReadOnlyDictionary<string, string> Headers { get; init; }
}
