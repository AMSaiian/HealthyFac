using HealthyFac.Auth.Application.Identities.IdentityAdmin;
using HealthyFac.Auth.Application.Identities;
using HealthyFac.Auth.Infrastructure.Keycloak;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using HealthyFac.Testing.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Auth.IntegrationTests.Keycloak.Data;

/// <summary>
/// Starts Keycloak with the development realm and Mailpit, points the SMTP settings of the realm at Mailpit, and
/// builds the admin client with the service account of the auth service. It also starts Redis, because the service
/// token client stores its tokens there.
/// </summary>
public sealed class KeycloakAdminFixture : IAsyncLifetime
{
    private const string RealmPath = "admin/realms/{0}";
    private const string MailpitSmtpFrom = "noreply@healthyfac.local";

    private readonly List<ServiceProvider> _extraServices = [];

    private ServiceProvider _services = null!;

    public KeycloakTestServer Keycloak { get; } = new();

    public MailpitTestServer Mailpit { get; } = new();

    public RedisTestServer Redis { get; } = new();

    /// <summary>
    /// The settings of the admin client and the service token. Read them after <see cref="InitializeAsync"/>.
    /// </summary>
    public IReadOnlyDictionary<string, string?> Settings { get; private set; } = new Dictionary<string, string?>();

    public KeycloakAdminApi Api => new(Keycloak);

    public IIdentityAdmin Admin => _services.GetRequiredService<IIdentityAdmin>();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            Keycloak.StartAsync(KeycloakAdminTestData.RealmEnvironment),
            Mailpit.StartAsync(),
            Redis.StartAsync());

        await Api.SendAsync(
            HttpMethod.Put,
            string.Format(RealmPath, KeycloakAdminTestData.RealmName),
            new
            {
                smtpServer = new
                {
                    host = KeycloakTestServer.HostGatewayName,
                    port = Mailpit.SmtpPort.ToString(),
                    from = MailpitSmtpFrom,
                },
            });

        var baseAddress = Keycloak.BaseAddress.TrimEnd('/');
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

        _services = services.BuildServiceProvider(true);
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
        var provider = services.BuildServiceProvider(true);
        _extraServices.Add(provider);

        return provider.GetRequiredService<IIdentityAdmin>();
    }

    public async Task DisposeAsync()
    {
        foreach (var extraServices in _extraServices)
        {
            await extraServices.DisposeAsync();
        }

        await _services.DisposeAsync();
        await Keycloak.DisposeAsync();
        await Mailpit.DisposeAsync();
        await Redis.DisposeAsync();
    }
}
