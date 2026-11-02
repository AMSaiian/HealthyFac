namespace HealthyFac.Testing.Authentication;

/// <summary>
/// Describes the claims of a test service token: the shape of a Client Credentials token of Keycloak. It has the
/// service account as <c>sub</c>, the <c>client_id</c>, and the <c>permissions</c> of the requested downstream
/// scopes. It has no <c>organization</c> and no <c>employee_id</c>. The defaults give a token of an allowed client
/// with one permission.
/// </summary>
public sealed record TestServiceTokenClaims
{
    public const string DefaultClientId = "healthyfac-auth";

    public static readonly Guid DefaultServiceAccountId = Guid.Parse("0190f3a0-0000-7000-8000-0000000000b1");

    public Guid Subject { get; init; } = DefaultServiceAccountId;

    public string ClientId { get; init; } = DefaultClientId;

    /// <summary>The <c>permissions</c> claim. The claim is left out when the list is empty, as Keycloak does.</summary>
    public IReadOnlyList<string> Permissions { get; init; } = [];

    public string Issuer { get; init; } = TestTokenFactory.Issuer;

    public string Audience { get; init; } = TestTokenFactory.Audience;

    public DateTimeOffset IssuedAt { get; init; } = DateTimeOffset.UtcNow;

    public TimeSpan Lifetime { get; init; } = TestTokenFactory.DefaultLifetime;
}
