using HealthyFac.MachineSimulator.Scenarios;

namespace HealthyFac.MachineSimulator.Simulation;

/// <summary>
/// A message that a tick built and the loop publishes after the engine lock is released.
/// </summary>
public sealed record PendingMessage
{
    /// <summary>The gateway ID of the topic.</summary>
    public required string GatewayId { get; init; }

    /// <summary>The scenario of the machine, for the metric tag.</summary>
    public required ScenarioKind Scenario { get; init; }

    /// <summary>The JSON bytes.</summary>
    public required byte[] Payload { get; init; }

    /// <summary>The metric keys of the sample, for the log.</summary>
    public required IReadOnlyList<string> Keys { get; init; }

    /// <summary>The status codes of the sample, for the log.</summary>
    public required IReadOnlyList<string> Codes { get; init; }
}
