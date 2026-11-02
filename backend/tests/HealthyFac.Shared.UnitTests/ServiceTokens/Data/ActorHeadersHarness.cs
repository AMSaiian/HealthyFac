using HealthyFac.Shared.Infrastructure.ServiceTokens;

namespace HealthyFac.Shared.UnitTests.ServiceTokens.Data;

/// <summary>
/// Wires a client through <see cref="ActorHeadersHandler"/> to a fake API, with a fake incoming request.
/// </summary>
public sealed class ActorHeadersHarness : IDisposable
{
    public ActorHeadersHarness()
    {
        Client = new HttpClient(new ActorHeadersHandler(Incoming) { InnerHandler = Resource });
    }

    public RecordingResourceHandler Resource { get; } = new();

    public FakeIncomingAccessTokenAccessor Incoming { get; } = new();

    public HttpClient Client { get; }

    public Task<HttpResponseMessage> CallAsync() => Client.GetAsync(ServiceTokenTestData.ResourceUrl);

    public void Dispose() => Client.Dispose();
}
