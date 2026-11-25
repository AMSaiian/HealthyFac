namespace HealthyFac.MachineSimulator.Scenarios.Payloads;

/// <summary>
/// The result of <see cref="MachinePayloadBuilder.Build"/>.
/// </summary>
public sealed record BuiltPayload
{
    /// <summary>The UTF-8 bytes of the payload.</summary>
    public required byte[] Json { get; init; }

    /// <summary>The keys that the builder left out because the value does not fit the value kind.</summary>
    public required IReadOnlyList<string> SkippedKeys { get; init; }
}
