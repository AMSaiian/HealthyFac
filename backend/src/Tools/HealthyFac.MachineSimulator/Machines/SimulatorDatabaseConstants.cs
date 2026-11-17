namespace HealthyFac.MachineSimulator.Machines;

/// <summary>
/// Names the connection strings and the health checks of the two databases that the machine simulator reads.
/// </summary>
public static class SimulatorDatabaseConstants
{
    public static class ConnectionStrings
    {
        public const string Core = "CoreDatabase";
        public const string Telemetry = "TelemetryDatabase";
    }

    public static class HealthChecks
    {
        public const string Core = "core-database";
        public const string Telemetry = "telemetry-database";
    }
}
