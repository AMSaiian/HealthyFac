using Flurl;

namespace HealthyFac.Auth.Infrastructure.Keycloak;

/// <summary>
/// Builds the paths of the Keycloak admin API, relative to the base address of the server.
/// </summary>
public static class KeycloakAdminRoutes
{
    private const string Admin = "admin/realms";
    private const string Organizations = "organizations";
    private const string Users = "users";
    private const string Roles = "roles";
    private const string Members = "members";
    private const string Credentials = "credentials";
    private const string ExecuteActionsEmail = "execute-actions-email";
    private const string Logout = "logout";
    private const string RealmRoleMappings = "role-mappings/realm";
    private const string SearchParameter = "search";
    private const string AttributeQueryParameter = "q";
    private const string ExactParameter = "exact";
    private const string ClientIdParameter = "client_id";
    private const string RedirectUriParameter = "redirect_uri";
    private const string LifespanParameter = "lifespan";

    public static string OrganizationSearch(string realm, string name)
        => RealmUrl(realm)
            .AppendPathSegment(Organizations)
            .SetQueryParam(SearchParameter, name)
            .SetQueryParam(ExactParameter, true)
            .ToString();

    public static string OrganizationMembers(string realm, Guid organizationId)
        => RealmUrl(realm).AppendPathSegments(Organizations, organizationId, Members).ToString();

    public static string UserSearchByAttribute(string realm, string attribute, string value)
        => RealmUrl(realm)
            .AppendPathSegment(Users)
            .SetQueryParam(AttributeQueryParameter, $"{attribute}:{value}")
            .SetQueryParam(ExactParameter, true)
            .ToString();

    public static string UsersRoot(string realm) => RealmUrl(realm).AppendPathSegment(Users).ToString();

    public static string User(string realm, Guid userId) => UserUrl(realm, userId).ToString();

    public static string UserCredentials(string realm, Guid userId)
        => UserUrl(realm, userId).AppendPathSegment(Credentials).ToString();

    public static string UserRealmRoleMappings(string realm, Guid userId)
        => UserUrl(realm, userId).AppendPathSegment(RealmRoleMappings).ToString();

    public static string UserLogout(string realm, Guid userId)
        => UserUrl(realm, userId).AppendPathSegment(Logout).ToString();

    public static string RealmRole(string realm, string roleName)
        => RealmUrl(realm).AppendPathSegment(Roles).AppendPathSegment(roleName, fullyEncode: true).ToString();

    public static string ExecuteActionsEmailRequest(
        string realm,
        Guid userId,
        string clientId,
        string redirectUri,
        int lifespanSeconds
    )
        => UserUrl(realm, userId)
            .AppendPathSegment(ExecuteActionsEmail)
            .SetQueryParam(ClientIdParameter, clientId)
            .SetQueryParam(RedirectUriParameter, redirectUri)
            .SetQueryParam(LifespanParameter, lifespanSeconds)
            .ToString();

    private static Url UserUrl(string realm, Guid userId) => RealmUrl(realm).AppendPathSegments(Users, userId);

    private static Url RealmUrl(string realm) => Admin.AppendPathSegment(realm, fullyEncode: true);
}
