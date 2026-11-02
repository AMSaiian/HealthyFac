namespace HealthyFac.HealthProbe;

/// <summary>
/// Defines the timeout, exit codes, and texts of the health probe.
/// </summary>
public static class ProbeConstants
{
    public const int TimeoutSeconds = 3;

    public static class ExitCodes
    {
        public const int Healthy = 0;
        public const int Unhealthy = 1;
        public const int InvalidUsage = 2;
    }

    public static class Messages
    {
        public const string Usage = "Usage: HealthyFac.HealthProbe <http-or-https-uri>";
        public const string Healthy = "Healthy ({0})";
        public const string Unhealthy = "Unhealthy ({0})";
    }
}
