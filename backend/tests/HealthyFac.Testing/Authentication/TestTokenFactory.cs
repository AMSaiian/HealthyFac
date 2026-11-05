using System.Security.Cryptography;
using HealthyFac.Shared.Web.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace HealthyFac.Testing.Authentication;

/// <summary>
/// Creates signed access tokens with the claim shape of Keycloak, so API and gateway tests need no Keycloak. The
/// factory holds one RSA key pair. <see cref="TrustTestKey"/> makes the JWT bearer handler of a test host trust
/// that key, instead of the signing keys of Keycloak.
/// </summary>
public sealed class TestTokenFactory
{
    public const string Issuer = "http://localhost:8000/realms/healthyfac";
    public const string MetadataAddress = "http://keycloak:8080/realms/healthyfac/.well-known/openid-configuration";
    public const string Audience = "healthyfac-core";
    public const string IsHttpsMetadataRequiredSetting = "Jwt:IsHttpsMetadataRequired";

    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(5);

    public static readonly IReadOnlyList<string> SupervisorPermissions =
    [
        "employees:invite",
        "employees:manage",
        "machines:read",
        "machines:write",
        "machines:delete",
        "machine-error-codes:read",
        "machine-error-codes:write",
        "machine-error-codes:delete",
    ];

    private const string KeyId = "test-key";
    private const int KeySizeInBits = 2048;

    private readonly RsaSecurityKey _key = new(RSA.Create(KeySizeInBits)) { KeyId = KeyId };

    /// <summary>The configuration values that make a host validate the tokens of this factory.</summary>
    public static IReadOnlyDictionary<string, string?> JwtSettings { get; } = new Dictionary<string, string?>
    {
        ["Jwt:Issuer"] = Issuer,
        ["Jwt:MetadataAddress"] = MetadataAddress,
        ["Jwt:Audience"] = Audience,
        [IsHttpsMetadataRequiredSetting] = bool.FalseString,
    };

    public string CreateToken(TestTokenClaims claims)
    {
        var tokenClaims = new Dictionary<string, object>
        {
            [KeycloakClaimTypes.Organization] = claims.Organizations.ToArray(),
            [KeycloakClaimTypes.Permissions] = claims.Permissions.ToArray(),
        };

        if (claims.Subject is not null)
        {
            tokenClaims[JwtRegisteredClaimNames.Sub] = claims.Subject.Value.ToString();
        }

        if (claims.EmployeeId is not null)
        {
            tokenClaims[KeycloakClaimTypes.EmployeeId] = claims.EmployeeId;
        }

        if (claims.DepartmentIds.Count > 0)
        {
            tokenClaims[KeycloakClaimTypes.DepartmentIds] = claims.DepartmentIds.ToArray();
        }

        return Sign(tokenClaims, claims.Issuer, claims.Audience, claims.IssuedAt, claims.Lifetime);
    }

    /// <summary>
    /// Creates a token with the shape of a Client Credentials token: no user, no organization, and no employee.
    /// </summary>
    /// <param name="claims">The claims of the service token.</param>
    /// <returns>The signed token.</returns>
    public string CreateServiceToken(TestServiceTokenClaims claims)
    {
        var tokenClaims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = claims.Subject.ToString(),
            [KeycloakClaimTypes.ClientId] = claims.ClientId,
        };

        if (claims.Permissions.Count > 0)
        {
            tokenClaims[KeycloakClaimTypes.Permissions] = claims.Permissions.ToArray();
        }

        return Sign(tokenClaims, claims.Issuer, claims.Audience, claims.IssuedAt, claims.Lifetime);
    }

    /// <summary>
    /// Post-configures the JWT bearer handler to read its signing keys from this factory, not from the metadata
    /// address. Register it with <c>PostConfigure&lt;JwtBearerOptions&gt;(JwtBearerDefaults.AuthenticationScheme, ...)</c>
    /// in the test host, so it runs after the application configuration.
    /// </summary>
    /// <param name="options">The bearer options of the host.</param>
    public void TrustTestKey(JwtBearerOptions options)
    {
        var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
        configuration.SigningKeys.Add(_key);

        options.Configuration = configuration;
        options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
        options.TokenValidationParameters.IssuerSigningKey = _key;
    }

    private string Sign(
        Dictionary<string, object> tokenClaims,
        string issuer,
        string audience,
        DateTimeOffset issuedAt,
        TimeSpan lifetime
    )
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = issuedAt.UtcDateTime + lifetime,
            Claims = tokenClaims,
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
