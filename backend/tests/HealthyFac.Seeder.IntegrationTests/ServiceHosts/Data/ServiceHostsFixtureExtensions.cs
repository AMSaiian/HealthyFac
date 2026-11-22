using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Shared.Infrastructure.ServiceTokens.Storage;
using HealthyFac.Telemetry.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.IntegrationTests.ServiceHosts.Data;

public static class ServiceHostsFixtureExtensions
{
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
                await client.GetFromJsonAsync<JsonArray>($"{path}?clientId={ServiceHostsTestData.AuthClientId}");
            var authClient = clients![index: 0]!.AsObject();
            var attributes = authClient[propertyName: "attributes"]!.AsObject();
            attributes[ServiceHostsTestData.TokenLifespanAttribute] = seconds?.ToString();

            using var response = await client.PutAsJsonAsync($"{path}/{authClient[propertyName: "id"]}", authClient);
            response.EnsureSuccessStatusCode();
        }
    }
}
