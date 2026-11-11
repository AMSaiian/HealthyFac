using System.Collections.Frozen;

namespace HealthyFac.Core.Web.Authorization;

/// <summary>
/// Defines the permissions of the Core API. Each permission is a client role of <c>healthyfac-core</c> in Keycloak,
/// and each endpoint policy has the name of its permission.
/// </summary>
public static class CorePermissions
{
    /// <summary>
    /// Holds every permission of the Core API. The service registers the policies from this set, so a permission
    /// that is missing here has no policy, and its endpoint fails with status 500.
    /// </summary>
    public static readonly FrozenSet<string> All =
        [.. Employees.All, .. Machines.All, .. Drafts.All, .. WorkOrders.All, .. Shifts.All];

    public static class Employees
    {
        public const string Read = "employees:read";
        public const string Invite = "employees:invite";
        public const string Manage = "employees:manage";

        /// <summary>
        /// Holds every permission of the employees feature. <see cref="CorePermissions.All"/> spreads this set, so a
        /// permission that is missing here has no policy.
        /// </summary>
        public static readonly FrozenSet<string> All = [Read, Invite, Manage];
    }

    public static class Machines
    {
        public const string Read = "machines:read";
        public const string Write = "machines:write";
        public const string Delete = "machines:delete";

        /// <summary>
        /// Holds every permission of the machines feature. <see cref="CorePermissions.All"/> spreads this set, so a
        /// permission that is missing here has no policy.
        /// </summary>
        public static readonly FrozenSet<string> All = [Read, Write, Delete];
    }

    public static class Drafts
    {
        public const string Read = "drafts:read";
        public const string Write = "drafts:write";

        /// <summary>
        /// Holds every permission of the drafts feature. <see cref="CorePermissions.All"/> spreads this set, so a
        /// permission that is missing here has no policy.
        /// </summary>
        public static readonly FrozenSet<string> All = [Read, Write];
    }

    public static class WorkOrders
    {
        public const string Read = "work-orders:read";
        public const string Write = "work-orders:write";
        public const string Manage = "work-orders:manage";

        /// <summary>
        /// Holds every permission of the work orders feature. <see cref="CorePermissions.All"/> spreads this set, so a
        /// permission that is missing here has no policy.
        /// </summary>
        public static readonly FrozenSet<string> All = [Read, Write, Manage];
    }

    public static class Shifts
    {
        public const string Read = "shifts:read";
        public const string Manage = "shifts:manage";

        /// <summary>
        /// Holds every permission of the shifts feature. <see cref="CorePermissions.All"/> spreads this set, so a
        /// permission that is missing here has no policy.
        /// </summary>
        public static readonly FrozenSet<string> All = [Read, Manage];
    }
}
