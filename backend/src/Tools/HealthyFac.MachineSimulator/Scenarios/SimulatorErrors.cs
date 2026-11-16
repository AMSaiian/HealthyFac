namespace HealthyFac.MachineSimulator.Scenarios;

/// <summary>
/// Defines the error messages of the control API.
/// </summary>
public static class SimulatorErrors
{
    public const string MachineNotFound = "The machine is not known to the simulator.";
    public const string ModuleNotFound = "The machine has no such module.";
    public const string ModeRequired = "The mode is required.";
    public const string ModeInvalid = "The mode must be expected, draining, or failure.";
    public const string RunRequired = "The isRunning flag is required.";

    public static string MissingConnectionString(string name) => $"The connection string is not configured: {name}";
}
