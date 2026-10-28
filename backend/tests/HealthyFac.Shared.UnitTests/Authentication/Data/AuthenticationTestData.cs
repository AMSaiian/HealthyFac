using HealthyFac.Shared.Web.Authentication;

namespace HealthyFac.Shared.UnitTests.Authentication.Data;

public static class AuthenticationTestData
{
    public const string Audience = "healthyfac-core";

    public static JwtOptions ValidOptions => CreateOptions();

    public static TheoryData<JwtOptions, string> InvalidOptions => new()
    {
        { CreateOptions(issuer: string.Empty), AuthenticationValidationMessages.IssuerInvalid },
        { CreateOptions(issuer: "healthyfac"), AuthenticationValidationMessages.IssuerInvalid },
        { CreateOptions(metadataAddress: string.Empty), AuthenticationValidationMessages.MetadataAddressInvalid },
        { CreateOptions(audience: " "), AuthenticationValidationMessages.AudienceMissing },
    };

    private static JwtOptions CreateOptions(
        string issuer = "http://localhost:8000/realms/healthyfac",
        string metadataAddress = "http://keycloak:8080/realms/healthyfac/.well-known/openid-configuration",
        string audience = Audience
    ) =>
        new()
        {
            Issuer = issuer,
            MetadataAddress = metadataAddress,
            Audience = audience,
        };
}
