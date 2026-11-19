namespace HealthyFac.Telemetry.Infrastructure.Evaluation.Persistence;

/// <summary>
/// Defines the database index names of the evaluation tables that code reads, so a name never differs between the
/// mapping and a check.
/// </summary>
public static class EvaluationIndexNames
{
    public const string MetricPhase = "ux_metric_statistics_metric_definition_id_operating_phase_id";
    public const string MachineRuleSubject = "ux_rule_streaks_machine_registration_id_rule_subject_id";
    public const string DueAt = "ix_machine_evaluations_due_at";
    public const string PhaseIntervalMachineStart = "ix_phase_intervals_machine_registration_id_started_at";
    public const string PhaseIntervalOpen = "ux_phase_intervals_machine_registration_id_open";
}
