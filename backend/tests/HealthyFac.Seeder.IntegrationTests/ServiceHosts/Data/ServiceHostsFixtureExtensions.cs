using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Shared.Infrastructure.HttpFailures;
using HealthyFac.Shared.Infrastructure.ServiceTokens.Storage;
using HealthyFac.Telemetry.Infrastructure.Database;
using HealthyFac.Testing.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.IntegrationTests.ServiceHosts.Data;

public static class ServiceHostsFixtureExtensions
{
    private const string RebusAssemblyPrefix = "Rebus";

    extension(ServiceDescriptor descriptor)
    {
        /// <summary>Tells whether the service type or the implementation type comes from a Rebus assembly.</summary>
        public bool IsFromRebusAssembly() =>
            new[]
                {
                    descriptor.ServiceType, descriptor.ImplementationType, descriptor.ImplementationInstance?.GetType(),
                }
                .Any(type => type?.Assembly.GetName()
                        .Name?.StartsWith(
                            RebusAssemblyPrefix,
                            StringComparison.Ordinal)
                    == true);
    }

    extension(SeederFixture fixture)
    {
        public async Task<int> CountRolesAsync()
        {
            using var scope = fixture.Hosts.Core.CreateScope(ServiceHostsTestData.Actor);

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>().Roles.CountAsync();
        }

        public async Task<int> CountMeasurementUnitsAsync()
        {
            using var scope = fixture.Hosts.Telemetry.CreateScope(ServiceHostsTestData.Actor);

            return await scope.ServiceProvider.GetRequiredService<TelemetryDbContext>().MeasurementUnits.CountAsync();
        }

        /// <summary>Reads a service token of the Auth host through its token cache (the Redis store).</summary>
        public Task<string> GetAuthTokenAsync(CancellationToken cancellationToken) =>
            fixture.Hosts.Auth.GetService<ServiceTokenCache>().GetTokenAsync([], cancellationToken);

        /// <summary>
        /// Sets the lifespan of the access tokens of the Auth client in Keycloak, or removes the setting.
        /// </summary>
        /// <param name="seconds">The lifespan in seconds, or <see langword="null"/> for the realm default.</param>
        public async Task SetAuthClientTokenLifespanAsync(int? seconds)
        {
            var adminToken = await fixture.Keycloak.GetAdminTokenAsync();
            using var client = new HttpClient { BaseAddress = new Uri(fixture.Keycloak.BaseAddress) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme: "Bearer", adminToken);
            var path = string.Format(ServiceHostsTestData.ClientsPathFormat, SeederTestData.KeycloakRealm);

            var clients =
                await client.GetJsonAsync<JsonArray>(
                    $"{path}?clientId={ServiceHostsTestData.AuthClientId}", TestContext.Current.CancellationToken);

            var authClient = clients![0]!.AsObject();
            var attributes = authClient["attributes"]!.AsObject();
            attributes[ServiceHostsTestData.TokenLifespanAttribute] = seconds?.ToString();

            using var response = await client.PutAsJsonAsync($"{path}/{authClient["id"]}", authClient);
            await response.EnsureSuccessAsync(TestContext.Current.CancellationToken);
        }
    }
}
