namespace HealthyFac.Telemetry.Domain.Intake;

/// <summary>
/// One error of a received message: where it happened and what it is. The path never holds a value from the payload
/// other than a metric key or a status code, which the machine definitions name.
/// </summary>
public sealed record IntakeError
{
    /// <summary>The JSON path of the error, for example <c>metrics.spindle.temperature</c>, or an empty path for the
    /// whole message.</summary>
    public required string Path { get; init; }

    /// <summary>One of <see cref="IntakeErrorCodes"/>.</summary>
    public required string Code { get; init; }
}
