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
    public static readonly FrozenSet<string> All = [.. Employees.All, .. Machines.All];

    public static class Employees
    {
        public const string Invite = "employees:invite";
        public const string Manage = "employees:manage";

        /// <summary>
        /// Holds every permission of the employees feature. <see cref="CorePermissions.All"/> spreads this set, so a
        /// permission that is missing here has no policy.
        /// </summary>
        public static readonly FrozenSet<string> All = [Invite, Manage];
    }

    public static class Machines
    {
        public const string Write = "machines:write";

        /// <summary>
        /// Holds every permission of the machines feature. <see cref="CorePermissions.All"/> spreads this set, so a
        /// permission that is missing here has no policy.
        /// </summary>
        public static readonly FrozenSet<string> All = [Write];
    }
}
