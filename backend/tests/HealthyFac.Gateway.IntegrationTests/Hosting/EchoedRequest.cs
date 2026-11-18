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

    /// <summary>The number of body bytes that the server read.</summary>
    public required long BodyLength { get; init; }

    /// <summary>The body text. Only the <see cref="StubApiServer"/> records it.</summary>
    public string Body { get; init; } = string.Empty;
}
