namespace HealthyFac.Telemetry.IntegrationTests.Intake.Data;

/// <summary>
/// One result of the intake and the way to make it happen.
/// </summary>
public sealed record IntakeResultCase
{
    public required string Result { get; init; }

    public string? ErrorCode { get; init; }

    /// <summary>The raw payload. A valid message when it is <see langword="null"/>.</summary>
    public string? Json { get; init; }

    /// <summary>Publishes to a topic above the topic limit, so the validator drops the message.</summary>
    public bool IsLongTopic { get; init; }

    /// <summary>Publishes the same message once before, so the second one is a duplicate.</summary>
    public bool IsRepeated { get; init; }

    /// <summary>Makes the first save fail with a failure that a repeat cannot fix.</summary>
    public bool IsFailureArmed { get; init; }
}
