using System.Collections.Frozen;

namespace HealthyFac.Telemetry.Web.Authorization;

/// <summary>
/// Defines the permissions of the Telemetry API. Each permission is a client role of <c>healthyfac-telemetry</c> in
/// Keycloak, and each endpoint policy has the name of its permission.
/// </summary>
public static class TelemetryPermissions
{
    /// <summary>
    /// Holds every permission of the Telemetry API. The service registers the policies from this set, so a
    /// permission that is missing here has no policy, and its endpoint fails with status 500.
    /// </summary>
    public static readonly FrozenSet<string> All = [.. MetricDefinitions.All, .. StatusDefinitions.All];

    public static class MetricDefinitions
    {
        public const string Read = "metric-definitions:read";
        public const string Write = "metric-definitions:write";

        /// <summary>
        /// Holds every permission of the metric definitions feature. <see cref="TelemetryPermissions.All"/> spreads
        /// this set, so a permission that is missing here has no policy.
        /// </summary>
        public static readonly FrozenSet<string> All = [Read, Write];
    }

    public static class StatusDefinitions
    {
        public const string Read = "status-definitions:read";
        public const string Write = "status-definitions:write";
        public const string Delete = "status-definitions:delete";

        /// <summary>
        /// Holds every permission of the status definitions feature. <see cref="TelemetryPermissions.All"/> spreads
        /// this set, so a permission that is missing here has no policy.
        /// </summary>
        public static readonly FrozenSet<string> All = [Read, Write, Delete];
    }
}
