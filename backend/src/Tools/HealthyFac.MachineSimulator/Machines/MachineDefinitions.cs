namespace HealthyFac.MachineSimulator.Machines;

/// <summary>
/// The metric definitions and the status codes of one machine, as the last state poll read them.
/// </summary>
public sealed record MachineDefinitions
{
    /// <summary>The active metric definitions.</summary>
    public required IReadOnlyList<MetricInfo> Metrics { get; init; }

    /// <summary>The codes of the active status definitions.</summary>
    public required IReadOnlyList<string> StatusCodes { get; init; }

    public static readonly MachineDefinitions Empty = new() { Metrics = [], StatusCodes = [] };
}
