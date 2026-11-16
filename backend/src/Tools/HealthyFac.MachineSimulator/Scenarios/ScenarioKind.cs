using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.MachineSimulator.Scenarios;

/// <summary>
/// The scenarios of the machine simulator. The camelCase values reach the control API.
/// </summary>
public sealed class ScenarioKind : Enumeration<ScenarioKind>
{
    public static readonly ScenarioKind CncTurningCenter = new(value: "cncTurningCenter");
    public static readonly ScenarioKind CentrifugalPump = new(value: "centrifugalPump");
    public static readonly ScenarioKind ScrewCompressor = new(value: "screwCompressor");
    public static readonly ScenarioKind Generic = new(value: "generic");

    private ScenarioKind(string value)
        : base(value)
    {
    }
}
