using System.Text.Json.Nodes;
using HealthyFac.Testing.Containers;

namespace HealthyFac.Auth.IntegrationTests.Keycloak.Data;

public static class KeycloakAdminFixtureExtensions
{
    private const string OrganizationsPath = "admin/realms/healthyfac/organizations";
    private const string UsersPath = "admin/realms/healthyfac/users";
    private static readonly TimeSpan MailTimeout = TimeSpan.FromSeconds(seconds: 30);
    private static readonly TimeSpan MailPollInterval = TimeSpan.FromMilliseconds(milliseconds: 250);

    extension(KeycloakAdminFixture fixture)
    {
        public async Task<Guid?> LoadOrganizationIdAsync(Guid companyId)
        {
            var organizations = await fixture.Api.GetAsync<List<OrganizationRepresentation>>(
                $"{OrganizationsPath}?search={companyId:D}&exact=true");

            return organizations.Select(organization => (Guid?)organization.Id).FirstOrDefault();
        }

        public async Task<IReadOnlyList<Guid>> LoadOrganizationMemberIdsAsync(Guid organizationId)
        {
            var members = await fixture.Api.GetAsync<List<OrganizationMemberRepresentation>>(
                $"{OrganizationsPath}/{organizationId}/members");

            return members.Select(member => member.Id).ToList();
        }

        public async Task<IReadOnlyList<string>> LoadRealmRoleNamesAsync(Guid userId)
        {
            var roles = await fixture.Api.GetAsync<List<RoleRepresentation>>(
                $"{UsersPath}/{userId}/role-mappings/realm");

            return roles.Select(role => role.Name).ToList();
        }

        public async Task<IReadOnlyList<string>> LoadUserAttributeValuesAsync(Guid userId, string attributeName)
        {
            var user = await fixture.Api.GetAsync<JsonObject>($"{UsersPath}/{userId}");

            return user[propertyName: "attributes"]?[attributeName]
                    ?.AsArray()
                    .Select(value => value!.GetValue<string>())
                    .ToList()
                ?? [];
        }

        public async Task<bool> IsUserEnabledAsync(Guid userId)
        {
            var user = await fixture.Api.GetAsync<JsonObject>($"{UsersPath}/{userId}");

            return user[propertyName: "enabled"]!.GetValue<bool>();
        }

        public Task SetPasswordAsync(Guid userId, string password)
            => fixture.Api.SendAsync(
                HttpMethod.Put,
                $"{UsersPath}/{userId}/reset-password",
                new { type = "password", value = password, temporary = false });

        public async Task<MailpitMessage> WaitForMailToAsync(string email)
        {
            using var timeout = new CancellationTokenSource(MailTimeout);
            while (true)
            {
                var messages = await fixture.Mailpit.GetMessagesAsync();
                var message = messages.FirstOrDefault(candidate => candidate.To.Contains(email));
                if (message is not null)
                {
                    return message;
                }

                await Task.Delay(MailPollInterval, timeout.Token);
            }
        }
    }

    private sealed record OrganizationRepresentation
    {
        public required Guid Id { get; init; }
    }

    private sealed record OrganizationMemberRepresentation
    {
        public required Guid Id { get; init; }
    }

    private sealed record RoleRepresentation
    {
        public required string Name { get; init; }
    }
}
