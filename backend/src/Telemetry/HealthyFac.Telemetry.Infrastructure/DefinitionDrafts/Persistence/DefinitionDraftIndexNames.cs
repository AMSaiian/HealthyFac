namespace HealthyFac.Telemetry.Infrastructure.DefinitionDrafts.Persistence;

/// <summary>
/// Defines the index names of the definition draft tables.
/// </summary>
public static class DefinitionDraftIndexNames
{
    public const string MetricAttempt = "ix_metric_definition_drafts_company_id_attempt_id";
    public const string MetricTarget = "ix_metric_definition_drafts_company_id_target_machine_id_state";
    public const string MetricEntry = "ix_metric_definition_drafts_proposal_entry_id";
    public const string StatusAttempt = "ix_status_definition_drafts_company_id_attempt_id";
    public const string StatusTarget = "ix_status_definition_drafts_company_id_target_machine_id_state";
    public const string StatusEntry = "ix_status_definition_drafts_proposal_entry_id";
    public const string PhaseAttempt = "ix_operating_phase_drafts_company_id_attempt_id";
    public const string PhaseTarget = "ix_operating_phase_drafts_company_id_target_machine_id_state";
    public const string PhaseEntry = "ix_operating_phase_drafts_proposal_entry_id";
}
