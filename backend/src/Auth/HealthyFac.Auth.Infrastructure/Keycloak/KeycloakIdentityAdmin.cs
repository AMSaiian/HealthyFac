using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Auth.Application.Identities;
using Microsoft.Extensions.Options;

namespace HealthyFac.Auth.Infrastructure.Keycloak;

/// <summary>
/// Calls the Keycloak admin REST API with the service account of the auth service. The HTTP client carries the
/// token (<c>AddClientCredentialsToken</c>). A failure status other than the documented ones throws
/// <see cref="HttpRequestException"/>, so the inbox retries the message.
/// </summary>
/// <param name="client">The client of the Keycloak server.</param>
/// <param name="options">Holds the realm and the web app values.</param>
public sealed class KeycloakIdentityAdmin(HttpClient client, IOptions<KeycloakAdminOptions> options) : IIdentityAdmin
{
    private readonly HttpClient _client = client;
    private readonly KeycloakAdminOptions _options = options.Value;

    public async Task<Guid?> FindOrganizationIdAsync(Guid companyId, CancellationToken ct)
    {
        var organizations = await GetRequiredAsync<List<KeycloakOrganization>>(
            KeycloakAdminRoutes.OrganizationSearch(
                _options.Realm,
                companyId.ToString(KeycloakAdminConstants.OrganizationNameFormat)),
            ct);

        return organizations.Select(organization => (Guid?)organization.Id).FirstOrDefault();
    }

    public async Task<Guid?> FindUserIdByEmployeeIdAsync(Guid employeeId, CancellationToken ct)
    {
        var users = await GetRequiredAsync<List<KeycloakUser>>(
            KeycloakAdminRoutes.UserSearchByAttribute(
                _options.Realm,
                KeycloakAdminConstants.EmployeeIdAttribute,
                employeeId.ToString()),
            ct);

        return users.Select(user => (Guid?)user.Id).FirstOrDefault();
    }

    public async Task<Guid> CreateUserAsync(NewIdentity identity, CancellationToken ct)
    {
        var request = new CreateKeycloakUserRequest
        {
            Username = identity.Email,
            Email = identity.Email,
            EmailVerified = true,
            Enabled = true,
            FirstName = identity.FirstName,
            LastName = identity.LastName,
            Attributes = new Dictionary<string, string[]>
            {
                [KeycloakAdminConstants.EmployeeIdAttribute] = [identity.EmployeeId.ToString()],
            },
        };

        using var response = await _client.PostAsJsonAsync(KeycloakAdminRoutes.UsersRoot(_options.Realm), request, ct);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw IdentityAdminErrors.IdentityConflict(identity.EmployeeId);
        }

        response.EnsureSuccessStatusCode();

        var location = response.Headers.Location;
        var lastSegment = location?.Segments.LastOrDefault()?.Trim(KeycloakAdminConstants.PathSeparator);

        return !Guid.TryParse(lastSegment, out var userId)
            ? throw IdentityAdminErrors.CreatedUserIdMissing()
            : userId;
    }

    public async Task AddOrganizationMemberAsync(Guid organizationId, Guid userId, CancellationToken ct)
    {
        using var response = await _client.PostAsJsonAsync(
            KeycloakAdminRoutes.OrganizationMembers(_options.Realm, organizationId),
            userId.ToString(),
            ct);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task AssignRealmRoleAsync(Guid userId, string roleName, CancellationToken ct)
    {
        using var roleResponse = await _client.GetAsync(KeycloakAdminRoutes.RealmRole(_options.Realm, roleName), ct);
        if (roleResponse.StatusCode == HttpStatusCode.NotFound)
        {
            throw IdentityAdminErrors.RealmRoleNotFound(roleName);
        }

        roleResponse.EnsureSuccessStatusCode();

        var role = await roleResponse.Content.ReadFromJsonAsync<KeycloakRole>(ct)
            ?? throw IdentityAdminErrors.ResponseEmpty();

        using var mapResponse = await _client.PostAsJsonAsync(
            KeycloakAdminRoutes.UserRealmRoleMappings(_options.Realm, userId),
            new[] { role },
            ct);
        mapResponse.EnsureSuccessStatusCode();
    }

    public async Task<bool> HasPasswordAsync(Guid userId, CancellationToken ct)
    {
        var credentials = await GetRequiredAsync<List<KeycloakCredential>>(
            KeycloakAdminRoutes.UserCredentials(_options.Realm, userId),
            ct);

        return credentials.Any(credential => credential.Type == KeycloakAdminConstants.PasswordCredentialType);
    }

    public async Task SendSetPasswordEmailAsync(Guid userId, CancellationToken ct)
    {
        using var response = await _client.PutAsJsonAsync(
            KeycloakAdminRoutes.ExecuteActionsEmailRequest(
                _options.Realm,
                userId,
                _options.SpaClientId,
                _options.SpaBaseUrl,
                _options.SetPasswordLifespanSeconds),
            new[] { KeycloakAdminConstants.UpdatePasswordAction },
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DisableUserAsync(Guid userId, CancellationToken ct)
    {
        var userPath = KeycloakAdminRoutes.User(_options.Realm, userId);
        var user = await GetRequiredAsync<JsonObject>(userPath, ct);
        user[KeycloakAdminConstants.EnabledProperty] = false;

        using var response = await _client.PutAsJsonAsync(userPath, user, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task LogoutUserAsync(Guid userId, CancellationToken ct)
    {
        using var response = await _client.PostAsync(
            KeycloakAdminRoutes.UserLogout(_options.Realm, userId),
            content: null,
            ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task<T> GetRequiredAsync<T>(string path, CancellationToken ct)
        where T : class
    {
        var body = await _client.GetFromJsonAsync<T>(path, ct);

        return body ?? throw IdentityAdminErrors.ResponseEmpty();
    }
}
