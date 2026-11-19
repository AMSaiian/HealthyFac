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
    public static readonly FrozenSet<string> All =
    [
        .. MetricDefinitions.All, .. StatusDefinitions.All, .. DefinitionDrafts.All, .. OperatingPhases.All,
        .. Signals.All,
    ];

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

    public static class OperatingPhases
    {
        public const string Read = "operating-phases:read";
        public const string Write = "operating-phases:write";

        /// <summary>
        /// Holds every permission of the operating phases feature. <see cref="TelemetryPermissions.All"/> spreads
        /// this set, so a permission that is missing here has no policy.
        /// </summary>
        public static readonly FrozenSet<string> All = [Read, Write];
    }

    public static class Signals
    {
        public const string Read = "signals:read";

        /// <summary>
        /// Holds every permission of the signals feature. <see cref="TelemetryPermissions.All"/> spreads this set, so
        /// a permission that is missing here has no policy.
        /// </summary>
        public static readonly FrozenSet<string> All = [Read];
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

    public static class DefinitionDrafts
    {
        public const string Read = "definition-drafts:read";
        public const string Write = "definition-drafts:write";

        /// <summary>
        /// Holds every permission of the definition drafts feature. <see cref="TelemetryPermissions.All"/> spreads
        /// this set, so a permission that is missing here has no policy.
        /// </summary>
        public static readonly FrozenSet<string> All = [Read, Write];
    }
}
