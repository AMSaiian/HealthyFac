using System.Net;
using HealthyFac.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Gateway.IntegrationTests.Hosting;

/// <summary>
/// Holds the Redis server that the tests of one class share. Each gateway the tests create gets its own client
/// address, so a test never inherits the rate limit counters of an earlier test.
/// </summary>
public sealed class GatewayEnvironment : IAsyncLifetime
{
    private const string ClientAddressPrefix = "203.0.113.";

    private readonly RedisTestServer _redis = new();
    private int _clientAddressCounter;

    public string RedisConnectionString => _redis.ConnectionString;

    public Task InitializeAsync() => _redis.StartAsync();

    public Task DisposeAsync() => _redis.DisposeAsync().AsTask();

    public IPAddress NextClientAddress() =>
        IPAddress.Parse(ClientAddressPrefix + Interlocked.Increment(ref _clientAddressCounter));

    public GatewayFactory CreateGateway(
        EchoServer core,
        IPAddress? clientAddress = null,
        IReadOnlyDictionary<string, string>? settings = null,
        Action<IServiceCollection>? configureServices = null
    ) => CreateGateway(core.Address, clientAddress, settings, configureServices);

    public GatewayFactory CreateGateway(
        string coreAddress,
        IPAddress? clientAddress = null,
        IReadOnlyDictionary<string, string>? settings = null,
        Action<IServiceCollection>? configureServices = null
    ) => new(RedisConnectionString, coreAddress, clientAddress ?? NextClientAddress(), settings, configureServices);
}
