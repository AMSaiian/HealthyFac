using HealthyFac.Auth.Application.Identities;
using HealthyFac.Auth.Infrastructure.Keycloak;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using HealthyFac.Testing.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Auth.IntegrationTests.Keycloak.Data;

/// <summary>
/// Starts Keycloak with the development realm and Mailpit, points the SMTP settings of the realm at Mailpit, and
/// builds the admin client with the service account of the auth service.
/// </summary>
public sealed class KeycloakAdminFixture : IAsyncLifetime
{
    private const string RealmPath = "admin/realms/{0}";
    private const string MailpitSmtpFrom = "noreply@healthyfac.local";

    private ServiceProvider _services = null!;

    public KeycloakTestServer Keycloak { get; } = new();

    public MailpitTestServer Mailpit { get; } = new();

    /// <summary>
    /// The settings of the admin client and the service token. Read them after <see cref="InitializeAsync"/>.
    /// </summary>
    public IReadOnlyDictionary<string, string?> Settings { get; private set; } = new Dictionary<string, string?>();

    public KeycloakAdminApi Api => new(Keycloak);

    public IIdentityAdmin Admin => _services.GetRequiredService<IIdentityAdmin>();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(Keycloak.StartAsync(KeycloakAdminTestData.RealmEnvironment), Mailpit.StartAsync());

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
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(Settings).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKeycloakIdentityAdmin(configuration);

        _services = services.BuildServiceProvider(validateScopes: true);
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        await Keycloak.DisposeAsync();
        await Mailpit.DisposeAsync();
    }
}
