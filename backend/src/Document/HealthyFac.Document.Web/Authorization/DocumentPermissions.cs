using System.Collections.Frozen;

namespace HealthyFac.Document.Web.Authorization;

/// <summary>
/// Defines the permissions of the Document API. Each permission is a client role of <c>healthyfac-document</c> in
/// Keycloak, and each endpoint policy has the name of its permission.
/// </summary>
public static class DocumentPermissions
{
    /// <summary>
    /// Holds every permission of the Document API. The service registers the policies from this set, so a
    /// permission that is missing here has no policy, and its endpoint fails with status 500.
    /// </summary>
    public static readonly FrozenSet<string> All = [.. Files.All, .. CompanyFiles.All];

    public static class Files
    {
        public const string Read = "files:read";
        public const string Write = "files:write";
        public const string Delete = "files:delete";

        /// <summary>
        /// Holds every permission of the machine files feature. <see cref="DocumentPermissions.All"/> spreads
        /// this set, so a permission that is missing here has no policy.
        /// </summary>
        public static readonly FrozenSet<string> All = [Read, Write, Delete];
    }

    public static class CompanyFiles
    {
        public const string Write = "company-files:write";
        public const string Delete = "company-files:delete";

        /// <summary>
        /// Holds every permission of the company files feature. <see cref="DocumentPermissions.All"/> spreads
        /// this set, so a permission that is missing here has no policy.
        /// </summary>
        public static readonly FrozenSet<string> All = [Write, Delete];
    }
}
