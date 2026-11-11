using System.Collections.Frozen;

namespace HealthyFac.Insights.Web.Authorization;

/// <summary>
/// Defines the permissions of the Insights API. Each permission is a client role of <c>healthyfac-insights</c> in
/// Keycloak, and each endpoint policy has the name of its permission.
/// </summary>
public static class InsightsPermissions
{
    /// <summary>
    /// Holds every permission of the Insights API. The service registers the policies from this set, so a
    /// permission that is missing here has no policy, and its endpoint fails with status 500.
    /// </summary>
    public static readonly FrozenSet<string> All = [.. Ingest.All];

    public static class Ingest
    {
        public const string Read = "ingest:read";
        public const string Write = "ingest:write";

        /// <summary>
        /// Holds every permission of the ingest feature. <see cref="InsightsPermissions.All"/> spreads this set, so
        /// a permission that is missing here has no policy.
        /// </summary>
        public static readonly FrozenSet<string> All = [Read, Write];
    }
}
