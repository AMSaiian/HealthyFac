namespace HealthyFac.Shared.Web.Authentication;

/// <summary>
/// Configures how a service validates the access tokens of Keycloak.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// The public issuer URL of the realm. It must equal the <c>iss</c> claim, which Keycloak builds from its public
    /// host name (the gateway).
    /// </summary>
    public required string Issuer { get; init; }

    /// <summary>
    /// The URL where the service reads the OpenID Connect metadata. It is the internal address of Keycloak, so
    /// the service needs no route through the gateway.
    /// </summary>
    public required string MetadataAddress { get; init; }

    /// <summary>The audience that a token must carry in <c>aud</c>.</summary>
    public required string Audience { get; init; }

    /// <summary>Set to false only for a development Keycloak without HTTPS.</summary>
    public bool IsHttpsMetadataRequired { get; init; } = true;
}
