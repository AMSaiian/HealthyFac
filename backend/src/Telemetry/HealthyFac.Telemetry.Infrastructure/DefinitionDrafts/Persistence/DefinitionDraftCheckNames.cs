namespace HealthyFac.Telemetry.Infrastructure.DefinitionDrafts.Persistence;

/// <summary>
/// Defines the names of the check constraints that the shared helpers cannot build, so a test reads the same name as
/// the mapping.
/// </summary>
public static class DefinitionDraftCheckNames
{
    public const string MetricTarget = "ck_metric_definition_drafts_target";
    public const string StatusTarget = "ck_status_definition_drafts_target";
    public const string MetricRange = "ck_metric_definition_drafts_min_below_max";
    public const string MetricValueKind = "ck_metric_definition_drafts_value_kind_allowed";
    public const string StatusSeverity = "ck_status_definition_drafts_severity_allowed";
    public const string EntryMachine = "ck_proposal_entries_machine_only_on_machine";

    /// <summary>
    /// A draft that waits for its target or was dismissed holds at most 1 target. A pending or applied draft holds
    /// exactly 1.
    /// </summary>
    public const string TargetSql =
        "(state IN ('awaitingTarget', 'dismissed') AND num_nonnulls(target_machine_id, target_machine_draft_id) <= 1) "
        + "OR (state NOT IN ('awaitingTarget', 'dismissed') "
        + "AND num_nonnulls(target_machine_id, target_machine_draft_id) = 1)";

    public const string RangeSql = "min IS NULL OR max IS NULL OR min < max";

    public const string EntryMachineSql = "(machine_id IS NOT NULL) = (state = 'onMachine')";
}
