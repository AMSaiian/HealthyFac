using HealthyFac.Auth.Application.Identities.IdentityAdmin;
using HealthyFac.Auth.Infrastructure.Keycloak;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using HealthyFac.Testing.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Auth.IntegrationTests.Keycloak.Data;

/// <summary>
/// Starts Keycloak with the development realm and Mailpit, and builds the admin client with the service account of the
/// auth service. It also starts Redis, because the service token client stores its tokens there.
/// </summary>
/// <remarks>
/// Keycloak and Mailpit share one Docker network, as in <c>docker-compose.yml</c>. Keycloak sends email to the SMTP
/// server of the realm file, the <c>mailpit</c> service on port 1025, which is the Mailpit network alias. The earlier
/// path through the Docker host and a published port lost connections under the load of a full run, and Keycloak then
/// failed the email with 500.
/// </remarks>
public sealed class KeycloakAdminFixture : IAsyncLifetime
{
    private readonly List<ServiceProvider> _extraServices = [];
    private readonly TestNetwork _network = new();

    private ServiceProvider _services = null!;

    public KeycloakAdminFixture()
    {
        Keycloak = new KeycloakTestServer(_network);
        Mailpit = new MailpitTestServer(_network);
    }

    public KeycloakTestServer Keycloak { get; }

    public MailpitTestServer Mailpit { get; }

    public RedisTestServer Redis { get; } = new();

    /// <summary>
    /// The settings of the admin client and the service token. Read them after <see cref="InitializeAsync"/>.
    /// </summary>
    public IReadOnlyDictionary<string, string?> Settings { get; private set; } = new Dictionary<string, string?>();

    public KeycloakAdminApi Api => new(Keycloak);

    public IIdentityAdmin Admin => _services.GetRequiredService<IIdentityAdmin>();

    public async ValueTask InitializeAsync()
    {
        await _network.CreateAsync();
        await Task.WhenAll(
            Keycloak.StartAsync(KeycloakAdminTestData.RealmEnvironment),
            Mailpit.StartAsync(),
            Redis.StartAsync());

        var baseAddress = Keycloak.BaseAddress.TrimEnd(trimChar: '/');
        Settings = new Dictionary<string, string?>
        {
            [$"{KeycloakAdminOptions.SectionName}:BaseAddress"] = baseAddress,
            [$"{KeycloakAdminOptions.SectionName}:Realm"] = KeycloakAdminTestData.RealmName,
            [$"{KeycloakAdminOptions.SectionName}:SpaClientId"] = KeycloakAdminTestData.SpaClientId,
            [$"{KeycloakAdminOptions.SectionName}:SpaBaseUrl"] = KeycloakAdminTestData.SpaUrl,
            [$"{ServiceTokenOptions.SectionName}:TokenEndpoint"] = string.Format(
                KeycloakAdminTestData.TokenPathFormat,
                baseAddress,
                KeycloakAdminTestData.RealmName),
            [$"{ServiceTokenOptions.SectionName}:ClientId"] = KeycloakAdminTestData.AuthClientId,
            [$"{ServiceTokenOptions.SectionName}:ClientSecret"] = KeycloakAdminTestData.AuthClientSecret,
            [$"{RedisOptions.SectionName}:ConnectionString"] = Redis.ConnectionString,
            [$"{ServiceOptions.SectionName}:Name"] = ServiceName.Auth.Value,
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(Settings).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddServiceOptions(configuration);
        services.AddSharedRedis(configuration);
        services.AddKeycloakIdentityAdmin(configuration);

        _services = services.BuildServiceProvider(validateScopes: true);
    }

    /// <summary>
    /// Builds another admin client with a different web app base URL, for example one that is not a registered
    /// redirect URI. The fixture disposes it.
    /// </summary>
    /// <param name="spaBaseUrl">The base URL of the web app that the client puts into the redirect URI.</param>
    /// <returns>The new admin client.</returns>
    public IIdentityAdmin CreateAdmin(string spaBaseUrl)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(Settings)
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{KeycloakAdminOptions.SectionName}:SpaBaseUrl"] = spaBaseUrl,
                })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddServiceOptions(configuration);
        services.AddSharedRedis(configuration);
        services.AddKeycloakIdentityAdmin(configuration);
        var provider = services.BuildServiceProvider(validateScopes: true);
        _extraServices.Add(provider);

        return provider.GetRequiredService<IIdentityAdmin>();
    }

    /// <summary>
    /// Deletes the users and clients that tests created in Keycloak and empties Redis, which holds the cached service
    /// token. The next call fetches a new token.
    /// </summary>
    public async ValueTask ResetAsync()
    {
        await Keycloak.DeleteTestIdentitiesAsync();
        await Redis.FlushAsync();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var extraServices in _extraServices)
        {
            await extraServices.DisposeAsync();
        }

        await _services.DisposeAsync();
        await Keycloak.DisposeAsync();
        await Mailpit.DisposeAsync();
        await Redis.DisposeAsync();
        await _network.DisposeAsync();
    }
}
