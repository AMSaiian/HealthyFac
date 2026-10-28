using System.Net;
using System.Net.Http.Headers;
using HealthyFac.Testing.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
    private const string BearerScheme = "Bearer";

    /// <summary>The origin of the web app. The base settings list no origin, so each host sets its own.</summary>
    public const string DefaultCorsOrigin = "http://app.healthyfac.test:4200";

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

    public TestTokenFactory Tokens { get; } = new();

    /// <summary>
    /// Creates a client that sends a signed access token. Without claims, the token is valid and belongs to a new user.
    /// </summary>
    /// <param name="claims">The claims of the token, or <see langword="null"/> for a new valid user.</param>
    /// <returns>An <see cref="HttpClient"/> with the bearer header set.</returns>
    public HttpClient CreateAuthenticatedClient(TestTokenClaims? claims = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            BearerScheme,
            Tokens.CreateToken(claims ?? new TestTokenClaims { Subject = Guid.NewGuid() }));

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(TestEnvironment);

        var overrides = new Dictionary<string, string?>(TestTokenFactory.JwtSettings)
        {
            [GatewaySettingKeys.RedisConnectionString] = _redisConnectionString,
            [GatewaySettingKeys.CorsAllowedOrigin] = DefaultCorsOrigin,
            [GatewaySettingKeys.CoreAddress] = _coreAddress,
            [GatewaySettingKeys.CoreHealthAddress] = _coreAddress,
        };

        foreach (var (key, value) in _settings)
        {
            overrides[key] = value;
        }

        foreach (var (key, value) in overrides)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(overrides));

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, Tokens.TrustTestKey);
            services.AddSingleton<IStartupFilter>(new ClientAddressStartupFilter(_clientAddress));
            _configureServices?.Invoke(services);
        });
    }
}
