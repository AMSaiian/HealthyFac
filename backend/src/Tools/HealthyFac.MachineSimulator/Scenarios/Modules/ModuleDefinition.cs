using HealthyFac.MachineSimulator.Scenarios.Modules.Effects;

namespace HealthyFac.MachineSimulator.Scenarios.Modules;

/// <summary>
/// A module of a scenario: what it does to the machine, how it changes the metrics while it drains, and which codes it
/// sends.
/// </summary>
public sealed record ModuleDefinition
{
    /// <summary>The name of the module.</summary>
    public required ModuleName Name { get; init; }

    /// <summary>A trip failure stops the machine. A warning failure keeps it running.</summary>
    public required ModuleKind Kind { get; init; }

    /// <summary>The status codes that the failure sends.</summary>
    public required IReadOnlyList<string> FailureCodes { get; init; }

    /// <summary>The changes to the metric values along the drain.</summary>
    public required IReadOnlyList<ModuleEffect> Effects { get; init; }

    /// <summary>The status codes that the drain sends while their rule holds.</summary>
    public required IReadOnlyList<CodeRule> DrainCodes { get; init; }
}
