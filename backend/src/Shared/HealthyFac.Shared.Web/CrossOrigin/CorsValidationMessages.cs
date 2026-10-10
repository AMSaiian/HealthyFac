namespace HealthyFac.Shared.Web.CrossOrigin;

public static class CorsValidationMessages
{
    public const string OriginsRequired =
        "Cross-origin support is enabled, so list at least one browser origin, or disable the section.";

    public const string OriginInvalid =
        "Each allowed origin must be '*' or an origin without a path, such as 'http://localhost:4200'.";

    public const string CredentialsWithWildcardOrigin =
        "Credentials cannot be combined with the '*' origin. Browsers reject that pair, so name each origin.";
}
