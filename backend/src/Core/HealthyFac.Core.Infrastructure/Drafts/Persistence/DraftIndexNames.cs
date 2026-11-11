namespace HealthyFac.Core.Infrastructure.Drafts.Persistence;

/// <summary>
/// Defines the index names of the draft tables.
/// </summary>
public static class DraftIndexNames
{
    public const string MachineDraftAttempt = "ix_machine_drafts_company_id_attempt_id";
    public const string MachineDraftTarget = "ix_machine_drafts_target_machine_id_state";
    public const string MetricAttempt = "ix_metric_definition_drafts_company_id_attempt_id";
    public const string MetricTarget = "ix_metric_definition_drafts_target_machine_id_state";
    public const string StatusAttempt = "ix_status_definition_drafts_company_id_attempt_id";
    public const string StatusTarget = "ix_status_definition_drafts_target_machine_id_state";
}
