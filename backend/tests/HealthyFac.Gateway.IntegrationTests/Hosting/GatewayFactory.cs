using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Gateway.IntegrationTests.Hosting;

/// <summary>
/// Starts the gateway in memory, pointed at a Redis server and at a stand-in for Core. The factory fixes the
/// address of the client, because the in-memory server sets none and the rate limiter partitions by it.
/// </summary>
/// <remarks>
/// The settings also enter as the last configuration source, so they win over environment variables that a
/// <c>.env</c> file sets on a developer machine.
/// </remarks>
public sealed class GatewayFactory : WebApplicationFactory<Program>
{
    private const string TestEnvironment = "Test";

    private readonly string _redisConnectionString;
    private readonly string _coreAddress;
    private readonly IPAddress _clientAddress;
    private readonly IReadOnlyDictionary<string, string> _settings;
    private readonly Action<IServiceCollection>? _configureServices;

    public GatewayFactory(
        string redisConnectionString,
        string coreAddress,
        IPAddress clientAddress,
        IReadOnlyDictionary<string, string>? settings = null,
        Action<IServiceCollection>? configureServices = null
    )
    {
        _redisConnectionString = redisConnectionString;
        _coreAddress = coreAddress;
        _clientAddress = clientAddress;
        _settings = settings ?? new Dictionary<string, string>();
        _configureServices = configureServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(TestEnvironment);

        var overrides = new Dictionary<string, string?>(_settings!)
        {
            [GatewaySettingKeys.RedisConnectionString] = _redisConnectionString,
            [GatewaySettingKeys.CoreAddress] = _coreAddress,
        };

        foreach (var (key, value) in overrides)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(overrides));

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupFilter>(new ClientAddressStartupFilter(_clientAddress));
            _configureServices?.Invoke(services);
        });
    }
}
