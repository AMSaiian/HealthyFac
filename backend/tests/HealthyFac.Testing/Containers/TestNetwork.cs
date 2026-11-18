using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;

namespace HealthyFac.Testing.Containers;

/// <summary>
/// One Docker network that several test containers of a fixture join, so they reach each other through their network
/// aliases instead of the Docker host and a published port.
/// </summary>
/// <remarks>
/// Create it before the containers start, and dispose it after them.
/// </remarks>
public sealed class TestNetwork : IAsyncDisposable
{
    internal INetwork Network { get; } = new NetworkBuilder().Build();

    public Task CreateAsync() => Network.CreateAsync();

    public ValueTask DisposeAsync() => Network.DisposeAsync();
}
