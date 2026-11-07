namespace HealthyFac.Telemetry.Application.Intake;

/// <summary>
/// The result of the read of a raw payload.
/// </summary>
public readonly record struct PayloadReading
{
    /// <summary>The payload as text, or <see langword="null"/> when it is unreadable.</summary>
    public string? Text { get; init; }

    /// <summary>
    /// The <see cref="IntakeErrorCodes"/> value of an unreadable payload, or <see langword="null"/> when the payload
    /// is readable.
    /// </summary>
    public string? ErrorCode { get; init; }
}
