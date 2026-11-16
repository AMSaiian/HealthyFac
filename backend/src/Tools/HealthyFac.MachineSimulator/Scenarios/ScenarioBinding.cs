namespace HealthyFac.MachineSimulator.Scenarios;

/// <summary>
/// Binds a machine to a scenario by its serial number. The three demo machines get a detailed scenario, and every other
/// machine gets the generic scenario.
/// </summary>
public static class ScenarioBinding
{
    public const string CncSerialNumber = "MTC420-24-0187";
    public const string PumpSerialNumber = "HV-EP80-2023-04412";
    public const string CompressorSerialNumber = "NRS37-22-1093";

    /// <summary>
    /// Finds the scenario kind of a serial number. The comparison ignores case.
    /// </summary>
    /// <param name="serialNumber">The serial number of the machine.</param>
    /// <returns>The kind of the scenario.</returns>
    public static ScenarioKind KindOf(string serialNumber) =>
        string.Equals(serialNumber, CncSerialNumber, StringComparison.OrdinalIgnoreCase)
            ? ScenarioKind.CncTurningCenter
            : string.Equals(serialNumber, PumpSerialNumber, StringComparison.OrdinalIgnoreCase)
                ? ScenarioKind.CentrifugalPump
                : string.Equals(serialNumber, CompressorSerialNumber, StringComparison.OrdinalIgnoreCase)
                    ? ScenarioKind.ScrewCompressor
                    : ScenarioKind.Generic;

    /// <summary>
    /// Creates the scenario of a kind.
    /// </summary>
    /// <param name="kind">The kind of the scenario.</param>
    /// <returns>The scenario.</returns>
    public static IMachineScenario Create(ScenarioKind kind) =>
        kind == ScenarioKind.CncTurningCenter
            ? new CncTurningCenterScenario()
            : kind == ScenarioKind.CentrifugalPump
                ? new CentrifugalPumpScenario()
                : kind == ScenarioKind.ScrewCompressor
                    ? new ScrewCompressorScenario()
                    : new GenericScenario();
}
