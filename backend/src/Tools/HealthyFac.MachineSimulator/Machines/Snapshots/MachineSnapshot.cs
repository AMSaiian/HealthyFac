using HealthyFac.Core.Domain.Machines;

namespace HealthyFac.MachineSimulator.Machines.Snapshots;

/// <summary>
/// What a poll reads about one machine: the Core data, the registration in Telemetry, and the definitions.
/// </summary>
public sealed record MachineSnapshot
{
    /// <summary>The machine ID.</summary>
    public required Guid MachineId { get; init; }

    /// <summary>The company of the machine.</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>The name of the machine.</summary>
    public required string Name { get; init; }

    /// <summary>The serial number, which binds the scenario.</summary>
    public required string SerialNumber { get; init; }

    /// <summary>The gateway ID of the MQTT topic.</summary>
    public required string GatewayId { get; init; }

    /// <summary>The first day of telemetry, in the company time zone.</summary>
    public required DateOnly ExpectedGoLiveOn { get; init; }

    /// <summary>The time zone of the company.</summary>
    public required TimeZoneInfo Zone { get; init; }

    /// <summary>The state rows of the machine.</summary>
    public required IReadOnlyList<StateHistoryEntry> StateHistory { get; init; }

    /// <summary>Whether Telemetry holds a registration of the machine.</summary>
    public required bool IsRegistered { get; init; }

    /// <summary>The metric definitions and the status codes.</summary>
    public required MachineDefinitions Definitions { get; init; }

    /// <summary>The state of the row with the latest start. A machine without a row counts as operational.</summary>
    public MachineState CurrentState =>
        StateHistory.MaxBy(entry => entry.StartedAt)?.State ?? MachineState.Operational;
}
