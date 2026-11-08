using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Auth.Application.Identities.IdentityAdmin;
using HealthyFac.Auth.Infrastructure.Keycloak.Representations;
using HealthyFac.Shared.Infrastructure.HttpFailures;
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

    public async Task<Guid?> FindOrganizationIdAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var organizations = await GetRequiredAsync<List<KeycloakOrganization>>(
            KeycloakAdminRoutes.OrganizationSearch(
                _options.Realm,
                companyId.ToString(KeycloakAdminConstants.OrganizationNameFormat)),
            cancellationToken);

        return organizations.Select(organization => (Guid?)organization.Id).FirstOrDefault();
    }

    public async Task<Guid?> FindUserIdByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var users = await GetRequiredAsync<List<KeycloakUser>>(
            KeycloakAdminRoutes.UserSearchByAttribute(
                _options.Realm,
                KeycloakAdminConstants.EmployeeIdAttribute,
                employeeId.ToString()),
            cancellationToken);

        return users.Select(user => (Guid?)user.Id).FirstOrDefault();
    }

    public async Task<Guid> CreateUserAsync(NewIdentity identity, CancellationToken cancellationToken)
    {
        var request = new CreateKeycloakUserRequest
        {
            Username = identity.Email,
            Email = identity.Email,
            IsEmailVerified = true,
            IsEnabled = true,
            FirstName = identity.FirstName,
            LastName = identity.LastName,
            Attributes = new Dictionary<string, string[]>
            {
                [KeycloakAdminConstants.EmployeeIdAttribute] = [identity.EmployeeId.ToString()],
            },
        };

        using var response = await _client.PostAsJsonAsync(
            KeycloakAdminRoutes.UsersRoot(_options.Realm), request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw IdentityAdminErrors.IdentityConflict(identity.EmployeeId);
        }

        await response.EnsureSuccessAsync(cancellationToken);

        var location = response.Headers.Location;
        var lastSegment = location?.Segments.LastOrDefault()?.Trim(KeycloakAdminConstants.PathSeparator);

        return !Guid.TryParse(lastSegment, out var userId)
            ? throw IdentityAdminErrors.CreatedUserIdMissing()
            : userId;
    }

    public async Task AddOrganizationMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        using var response = await _client.PostAsJsonAsync(
            KeycloakAdminRoutes.OrganizationMembers(_options.Realm, organizationId),
            userId.ToString(),
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return;
        }

        await response.EnsureSuccessAsync(cancellationToken);
    }

    public async Task AssignRealmRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken)
    {
        using var roleResponse = await _client.GetAsync(
            KeycloakAdminRoutes.RealmRole(_options.Realm, roleName), cancellationToken);
        if (roleResponse.StatusCode == HttpStatusCode.NotFound)
        {
            throw IdentityAdminErrors.RealmRoleNotFound(roleName);
        }

        await roleResponse.EnsureSuccessAsync(cancellationToken);

        var role = await roleResponse.Content.ReadFromJsonAsync<KeycloakRole>(cancellationToken)
            ?? throw IdentityAdminErrors.ResponseEmpty();

        using var mapResponse = await _client.PostAsJsonAsync(
            KeycloakAdminRoutes.UserRealmRoleMappings(_options.Realm, userId),
            new[] { role },
            cancellationToken);
        await mapResponse.EnsureSuccessAsync(cancellationToken);
    }

    public async Task<bool> HasPasswordAsync(Guid userId, CancellationToken cancellationToken)
    {
        var credentials = await GetRequiredAsync<List<KeycloakCredential>>(
            KeycloakAdminRoutes.UserCredentials(_options.Realm, userId),
            cancellationToken);

        return credentials.Any(credential => credential.Type == KeycloakAdminConstants.PasswordCredentialType);
    }

    public async Task SendSetPasswordEmailAsync(Guid userId, CancellationToken cancellationToken)
    {
        using var response = await _client.PutAsJsonAsync(
            KeycloakAdminRoutes.ExecuteActionsEmailRequest(
                _options.Realm,
                userId,
                _options.SpaClientId,
                _options.SpaBaseUrl,
                _options.SetPasswordLifespanSeconds),
            new[] { KeycloakAdminConstants.UpdatePasswordAction },
            cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);
    }

    public async Task DisableUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var userPath = KeycloakAdminRoutes.User(_options.Realm, userId);
        var user = await GetRequiredAsync<JsonObject>(userPath, cancellationToken);
        user[KeycloakAdminConstants.EnabledProperty] = false;

        using var response = await _client.PutAsJsonAsync(userPath, user, cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);
    }

    public async Task LogoutUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        using var response = await _client.PostAsync(
            KeycloakAdminRoutes.UserLogout(_options.Realm, userId),
            content: null,
            cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);
    }

    private async Task<T> GetRequiredAsync<T>(string path, CancellationToken cancellationToken)
        where T : class
    {
        using var response = await _client.GetAsync(path, cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);

        var body = await response.Content.ReadFromJsonAsync<T>(cancellationToken);

        return body ?? throw IdentityAdminErrors.ResponseEmpty();
    }
}
