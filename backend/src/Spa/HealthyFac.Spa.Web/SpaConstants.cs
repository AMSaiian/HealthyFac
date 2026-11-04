namespace HealthyFac.Spa.Web;

public static class SpaConstants
{
    public const string NoncePlaceholder = "__CSP_NONCE__";
    public const string IndexFileName = "index.html";
    public const string RuntimeConfigRoute = "/config.json";
    public const string SilentSsoFileName = "silent-check-sso.html";

    public static class CacheControl
    {
        public const string NoCache = "no-cache";
        public const string PublicMaxAgeFormat = "public, max-age={0}";
        public const string ImmutableFormat = "public, max-age={0}, immutable";
    }

    public static class HashedFiles
    {
        public const int MinCacheSeconds = 1;
        public const int MaxCacheSeconds = 31536000;

        /// <summary>
        /// The limit for one match of the hash pattern. The pattern comes from configuration, so a slow pattern
        /// must never block a request.
        /// </summary>
        public static readonly TimeSpan PatternMatchTimeout = TimeSpan.FromMilliseconds(100);
    }

    public static class RuntimeConfig
    {
        public const int MinCacheSeconds = 1;
        public const int MaxCacheSeconds = 3600;
    }

    public static class SecurityHeaders
    {
        public const string ReferrerPolicyName = "Referrer-Policy";
        public const string PermissionsPolicyName = "Permissions-Policy";
        public const string ContentTypeOptions = "nosniff";
        public const string ReferrerPolicy = "strict-origin-when-cross-origin";
    }
}
