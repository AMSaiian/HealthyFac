using HealthyFac.MachineSimulator.Scenarios;

namespace HealthyFac.MachineSimulator.UnitTests.Scenarios.Data;

public static class ScenarioBindingTestData
{
    public static TheoryData<string, string, ScenarioKind> Bindings =>
        new()
        {
            { "CncExact", "MTC420-24-0187", ScenarioKind.CncTurningCenter },
            { "CncLower", "mtc420-24-0187", ScenarioKind.CncTurningCenter },
            { "PumpUpper", "HV-EP80-2023-04412", ScenarioKind.CentrifugalPump },
            { "PumpMixed", "hv-Ep80-2023-04412", ScenarioKind.CentrifugalPump },
            { "CompressorLower", "nrs37-22-1093", ScenarioKind.ScrewCompressor },
            { "Unknown", "ABC-1", ScenarioKind.Generic },
            { "NearMiss", "MTC420-24-0188", ScenarioKind.Generic },
        };
}
