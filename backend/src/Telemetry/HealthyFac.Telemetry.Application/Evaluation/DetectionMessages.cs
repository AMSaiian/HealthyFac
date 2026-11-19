namespace HealthyFac.Telemetry.Application.Evaluation;

/// <summary>
/// Defines the validation messages of <see cref="DetectionOptions"/>.
/// </summary>
public static class DetectionMessages
{
    public const string ValueNotPositive = "Every detection value must be above 0.";
    public const string FastTauNotBelowSlowTau = "FastTauSeconds must be below SlowTauSeconds.";
    public const string DriftResolveLimitNotBelowDriftLimit = "DriftResolveLimit must be below DriftLimit.";

    public const string PhaseDwellNotBelowUnknownModeDwell =
        "PhaseDwellSeconds must be below UnknownModeDwellSeconds.";
}
