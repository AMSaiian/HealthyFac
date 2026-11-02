using HealthyFac.Auth.Application.Identities;
using HealthyFac.Shared.Infrastructure.Configuration;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Auth.Infrastructure.Keycloak;

/// <summary>
/// Provides extension methods for registering the Keycloak admin client.
/// </summary>
public static class KeycloakExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the validated Keycloak admin options, the service token handler, and
        /// <see cref="IIdentityAdmin"/> as a typed HTTP client that sends the token of the service account.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddKeycloakIdentityAdmin(IConfiguration configuration)
        {
            services.AddServiceTokens(configuration);
            services.AddValidatedOptions<KeycloakAdminOptions, KeycloakAdminOptionsValidator>(
                KeycloakAdminOptions.SectionName,
                configuration);

            services.AddHttpClient<IIdentityAdmin, KeycloakIdentityAdmin>((provider, client) =>
                {
                    var options = provider.GetRequiredService<IOptions<KeycloakAdminOptions>>().Value;
                    client.BaseAddress = new Uri(
                        $"{options.BaseAddress.TrimEnd(KeycloakAdminConstants.PathSeparator)}"
                        + $"{KeycloakAdminConstants.PathSeparator}");
                    client.Timeout = TimeSpan.FromSeconds(KeycloakAdminConstants.RequestTimeoutSeconds);
                })
                .AddClientCredentialsToken();

            return services;
        }
    }
}
