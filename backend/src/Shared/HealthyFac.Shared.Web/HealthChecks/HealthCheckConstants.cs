namespace HealthyFac.Shared.Web.HealthChecks;

/// <summary>
/// Defines routes, tags, and payload keys for the health check endpoints. The service serves the routes on the
/// health port only (see <see cref="HealthEndpointOptions"/>).
/// </summary>
public static class HealthCheckConstants
{
    /// <summary>Marks checks that decide whether the service can serve traffic.</summary>
    public const string ReadyTag = "ready";

    public static class Routes
    {
        /// <summary>Liveness route on the health port. Runs no checks and always answers 200 for a pull hook.</summary>
        public const string Liveness = "/health";

        /// <summary>Readiness route on the health port. Runs only the checks tagged <see cref="ReadyTag"/>.</summary>
        public const string Readiness = "/health/ready";

        /// <summary>Detailed route on the health port. Runs every check and reports each result.</summary>
        public const string Detailed = "/hc";
    }

    public static class Properties
    {
        public const string Status = "status";
        public const string TotalDuration = "totalDurationMs";
        public const string Checks = "checks";
        public const string Name = "name";
        public const string Description = "description";
        public const string Duration = "durationMs";
        public const string Error = "error";
        public const string Tags = "tags";
    }
}
