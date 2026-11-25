using HealthyFac.Telemetry.Domain.Intake;

namespace HealthyFac.Telemetry.Application.Intake;

/// <summary>
/// The readings of an accepted machine message and its errors, in the form that the intake stores.
/// </summary>
public sealed record AcceptedReadings
{
    public required IReadOnlyList<NumericReading> Numeric { get; init; }

    public required IReadOnlyList<DiscreteReading> Discrete { get; init; }

    public required IReadOnlyList<StatusReading> Statuses { get; init; }

    /// <summary>The errors of the parser and the unknown status codes.</summary>
    public required IReadOnlyList<IntakeError> Errors { get; init; }
}
