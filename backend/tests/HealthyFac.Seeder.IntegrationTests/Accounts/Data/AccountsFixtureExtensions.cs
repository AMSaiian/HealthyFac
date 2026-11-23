using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Auth.Application.Identities.IdentityAdmin;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Shared.Infrastructure.HttpFailures;
using HealthyFac.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.IntegrationTests.Accounts.Data;

public static class AccountsFixtureExtensions
{
    extension(SeederFixture fixture)
    {
        public async Task<Guid?> FindOrganizationAsync(Guid companyId)
        {
            using var scope = fixture.Hosts.Auth.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<IIdentityAdmin>()
                .FindOrganizationIdAsync(companyId, CancellationToken.None);
        }

        public async Task<Guid?> FindUserAsync(Guid employeeId)
        {
            using var scope = fixture.Hosts.Auth.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<IIdentityAdmin>()
                .FindUserIdByEmployeeIdAsync(employeeId, CancellationToken.None);
        }

        public async Task<bool> HasPasswordAsync(Guid userId)
        {
            using var scope = fixture.Hosts.Auth.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<IIdentityAdmin>()
                .HasPasswordAsync(userId, CancellationToken.None);
        }

        /// <summary>Signs in with a password grant and returns the claims of the access token.</summary>
        public async Task<JsonObject> SignInAsync(string email, string password)
        {
            var clientId = await fixture.Keycloak.CreatePasswordGrantClientAsync();
            using var client = new HttpClient { BaseAddress = new Uri(fixture.Keycloak.BaseAddress) };
            using var response = await client.PostAsync(
                string.Format(
                        SeederTestData.TokenPathFormat,
                        string.Empty,
                        SeederTestData.KeycloakRealm)
                    .TrimStart(trimChar: '/'),
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        [key: "grant_type"] = "password",
                        [key: "client_id"] = clientId,
                        [key: "client_secret"] = KeycloakTestServer.PasswordGrantClientSecret,
                        [key: "username"] = email,
                        [key: "password"] = password,
                        [key: "scope"] = "openid organization",
                    }));
            await response.EnsureSuccessAsync(TestContext.Current.CancellationToken);

            var body = await response.Content.ReadFromJsonAsync<JsonObject>();
            var token = body![AccountsTestData.AccessTokenProperty]!.GetValue<string>();

            return DecodePayload(token);
        }

        public async Task AddMemberAsync(Guid organizationId, Guid userId)
        {
            using var scope = fixture.Hosts.Auth.CreateScope(SeederTestData.InitializationActor);

            await scope.ServiceProvider.GetRequiredService<IIdentityAdmin>()
                .AddOrganizationMemberAsync(organizationId, userId, CancellationToken.None);
        }

        public async Task<int> CountUsersAsync(IEnumerable<Guid> employeeIds)
        {
            var count = 0;
            foreach (var employeeId in employeeIds)
            {
                count += await fixture.FindUserAsync(employeeId) is null ? 0 : 1;
            }

            return count;
        }
    }

    private static JsonObject DecodePayload(string token)
    {
        var payload = token.Split(separator: '.')[1]
            .Replace(oldChar: '-', newChar: '+')
            .Replace(oldChar: '_', newChar: '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, paddingChar: '=');

        return JsonNode.Parse(Convert.FromBase64String(payload))!.AsObject();
    }
}
