namespace HealthyFac.Spa.Web.Hosting;

/// <summary>
/// Defines the validator messages for the SPA host configuration.
/// </summary>
public static class SpaValidationMessages
{
    public const string ContentRootRequired = "The content root must not be blank.";
    public const string ApiBaseUrlMustBeHttps = "The API base URL must be an absolute https URL.";
    public const string ApiBaseUrlMustNotBeLoopback = "The API base URL must not use localhost or a loopback address.";
    public const string ApiBaseUrlMustNotEndWithSlash = "The API base URL must not end with a slash.";
    public const string AuthUrlMustBeHttps = "The auth URL must be an absolute https URL.";
    public const string AuthUrlMustNotBeLoopback = "The auth URL must not use localhost or a loopback address.";
    public const string AuthUrlMustNotEndWithSlash = "The auth URL must not end with a slash.";
    public const string AuthRealmRequired = "The auth realm must not be blank.";
    public const string AuthClientIdRequired = "The auth client ID must not be blank.";
    public const string HashedFileNamePatternRequired = "The hashed file name pattern must not be blank.";

    public const string HashedFileNamePatternInvalid =
        "The hashed file name pattern must be a valid regular expression.";

    public const string PermissionsPolicyRequired = "The permissions policy must not be blank.";

    public static readonly string RuntimeConfigCacheSecondsOutOfRange =
        $"The runtime config cache seconds must be from {SpaConstants.RuntimeConfig.MinCacheSeconds} "
        + $"to {SpaConstants.RuntimeConfig.MaxCacheSeconds}.";

    public static readonly string ImmutableCacheSecondsOutOfRange =
        $"The immutable cache seconds must be from {SpaConstants.HashedFiles.MinCacheSeconds} "
        + $"to {SpaConstants.HashedFiles.MaxCacheSeconds}.";
}
