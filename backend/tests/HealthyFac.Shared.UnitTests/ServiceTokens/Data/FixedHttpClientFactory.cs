namespace HealthyFac.Shared.UnitTests.ServiceTokens.Data;

/// <summary>
/// Gives every client the same handler, so a test plays the token endpoint without a network.
/// </summary>
/// <param name="handler">The handler that answers every request.</param>
public sealed class FixedHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler = handler;

    public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
}
