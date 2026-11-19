namespace HealthyFac.Telemetry.Infrastructure.Evaluation.Persistence;

/// <summary>
/// Defines the constraint names and the SQL of the phase interval table.
/// </summary>
public static class PhaseIntervalConstants
{
    public const string EndNotBeforeStartConstraintName = "ck_phase_intervals_ended_at_not_before_started_at";
    public const string EndNotBeforeStartCheck = "ended_at IS NULL OR ended_at >= started_at";
    public const string OpenFilter = "ended_at IS NULL";
}
