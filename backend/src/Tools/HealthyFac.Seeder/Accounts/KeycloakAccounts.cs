using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Auth.Application.Identities.IdentityAdmin;
using HealthyFac.Auth.Infrastructure.Keycloak;
using HealthyFac.Seeder.ServiceHosts;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Infrastructure.HttpFailures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Seeder.Accounts;

/// <summary>
/// Creates the Keycloak organization and the users of the seed. It uses the HTTP client that the Auth host registers
/// for <see cref="IIdentityAdmin"/> (the admin base address and the client credentials token), so the seed reaches
/// Keycloak the way the Auth service does. The user is created with a password, so the later
/// <c>ProvisionIdentity</c> of Auth finds it and sends no set-password email.
/// </summary>
public sealed class KeycloakAccounts
{
    private readonly ServiceHost _auth;

    public KeycloakAccounts(ServiceHost auth)
    {
        _auth = auth;
    }

    /// <summary>Creates the organization of a company. Its name and alias are the company ID.</summary>
    /// <param name="companyId">The company ID.</param>
    /// <param name="name">The display name of the company, kept as the description of the organization.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The ID of the organization.</returns>
    public async Task<Guid> CreateOrganizationAsync(Guid companyId, string name, CancellationToken cancellationToken)
    {
        using var scope = _auth.CreateScope(Actor(_auth));
        var options = scope.ServiceProvider.GetRequiredService<IOptions<KeycloakAdminOptions>>().Value;
        var client = CreateClient(scope);
        var organizationName = companyId.ToString(AccountConstants.OrganizationNameFormat);

        using var response = await client.PostAsJsonAsync(
            string.Format(AccountConstants.OrganizationsPath, options.Realm),
            new JsonObject
            {
                ["name"] = organizationName,
                ["alias"] = organizationName,
                ["enabled"] = true,
                ["description"] = name,
                ["domains"] = new JsonArray(),
            },
            cancellationToken);

        await response.EnsureSuccessAsync(cancellationToken);

        var lastSegment = response.Headers.Location?.Segments.LastOrDefault()?.Trim(trimChar: '/');

        return Guid.TryParse(lastSegment, out var organizationId)
            ? organizationId
            : throw new InvalidOperationException(AccountErrors.OrganizationIdMissing);
    }

    /// <summary>Creates a user with a permanent password and a verified email.</summary>
    /// <param name="employeeId">The employee ID that the user carries as an attribute.</param>
    /// <param name="email">The email, which is also the username.</param>
    /// <param name="firstName">The first name.</param>
    /// <param name="lastName">The last name.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The ID of the Keycloak user.</returns>
    public async Task<Guid> CreateUserAsync(
        Guid employeeId,
        string email,
        string firstName,
        string lastName,
        string password,
        CancellationToken cancellationToken
    )
    {
        using var scope = _auth.CreateScope(Actor(_auth));
        var options = scope.ServiceProvider.GetRequiredService<IOptions<KeycloakAdminOptions>>().Value;

        var userId = await scope.ServiceProvider.GetRequiredService<IIdentityAdmin>()
            .CreateUserAsync(
                new NewIdentity
                {
                    EmployeeId = employeeId,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                },
                cancellationToken);

        using var response = await CreateClient(scope)
            .PutAsJsonAsync(
                $"{KeycloakAdminRoutes.User(options.Realm, userId)}/{AccountConstants.ResetPasswordSuffix}",
                new JsonObject
                {
                    ["type"] = AccountConstants.PasswordCredentialType,
                    ["value"] = password,
                    ["temporary"] = false,
                },
                cancellationToken);

        await response.EnsureSuccessAsync(cancellationToken);

        return userId;
    }

    /// <summary>Deletes the members of the organization of a company, then the organization.</summary>
    /// <param name="companyId">The company ID.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the organization is gone. A missing organization is no error.</returns>
    public async Task DeleteOrganizationWithMembersAsync(Guid companyId, CancellationToken cancellationToken)
    {
        using var scope = _auth.CreateScope(Actor(_auth));
        var options = scope.ServiceProvider.GetRequiredService<IOptions<KeycloakAdminOptions>>().Value;

        var organizationId = await scope.ServiceProvider.GetRequiredService<IIdentityAdmin>()
            .FindOrganizationIdAsync(companyId, cancellationToken);

        if (organizationId is null)
        {
            return;
        }

        var client = CreateClient(scope);

        var membersPath = $"{KeycloakAdminRoutes.OrganizationMembers(options.Realm, organizationId.Value)}"
            + $"?{AccountConstants.MaxQueryParameter}={AccountConstants.MemberPageSize}";

        while (true)
        {
            var members = await ReadMembersAsync(client, membersPath, cancellationToken);

            if (members.Count == 0)
            {
                break;
            }

            foreach (var member in members)
            {
                using var userResponse = await client.DeleteAsync(
                    KeycloakAdminRoutes.User(options.Realm, Guid.Parse(member["id"]!.GetValue<string>())),
                    cancellationToken);

                await userResponse.EnsureSuccessAsync(cancellationToken);
            }
        }

        using var response = await client.DeleteAsync(
            string.Format(AccountConstants.OrganizationPath, options.Realm, organizationId.Value),
            cancellationToken);

        await response.EnsureSuccessAsync(cancellationToken);
    }

    private static async Task<List<JsonObject>> ReadMembersAsync(
        HttpClient client,
        string path,
        CancellationToken cancellationToken
    )
    {
        using var response = await client.GetAsync(path, cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);

        return await response.Content.ReadFromJsonAsync<List<JsonObject>>(cancellationToken) ?? [];
    }

    private static ActorContext Actor(ServiceHost auth) => ServiceActor.DatabaseInitialization.For(auth.Name);

    private static HttpClient CreateClient(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IIdentityAdmin));
}
