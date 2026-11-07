using HealthyFac.Telemetry.Domain.Intake;

namespace HealthyFac.Telemetry.Application.Intake;

/// <summary>
/// The result of the check of a machine message against the stored schema of its machine.
/// </summary>
public sealed record ParsedMachineMessage
{
    /// <summary>The errors of the message. An envelope failure has errors and no values.</summary>
    public required IReadOnlyList<IntakeError> Errors { get; init; }

    /// <summary>The sampling time as a UTC instant, or <see langword="null"/> when it is unreadable.</summary>
    public DateTime? SampledAt { get; init; }

    /// <summary><see langword="true"/> when the envelope failed, so no reading may be stored.</summary>
    public required bool IsEnvelopeFailure { get; init; }

    public IReadOnlyList<ParsedMetricValue> Metrics { get; init; } = [];

    /// <summary>The status codes of the message, each once, in the order of the message.</summary>
    public IReadOnlyList<string> StatusCodes { get; init; } = [];
}

/// <summary>
/// One valid value of a metric: a number, or text for a state or a boolean.
/// </summary>
public sealed record ParsedMetricValue
{
    public required string Key { get; init; }

    public double? Number { get; init; }

    public string? Text { get; init; }

    public required bool IsOutOfRange { get; init; }
}
