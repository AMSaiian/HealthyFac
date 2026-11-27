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
    public const string MetricBehavior = "ck_metric_definition_drafts_behavior_allowed";
    public const string MetricWarningLow = "ck_metric_definition_drafts_warning_low_not_below_alarm_low";
    public const string MetricWarningHigh = "ck_metric_definition_drafts_warning_high_not_above_alarm_high";
    public const string StatusSeverity = "ck_status_definition_drafts_severity_allowed";
    public const string PhaseTarget = "ck_operating_phase_drafts_target";
    public const string PhaseConditionSource = "ck_operating_phase_draft_conditions_exactly_one_source";
    public const string PhaseLimitMetric = "ck_operating_phase_draft_limits_exactly_one_metric";
    public const string EntryMachine = "ck_proposal_entries_machine_only_on_machine";
    public const string ProposalSourceFields = "ck_draft_proposals_source_fields";

    /// <summary>
    /// A draft that waits for its target or was dismissed holds at most 1 target. A pending or applied draft holds
    /// exactly 1.
    /// </summary>
    public const string TargetSql =
        "(state IN ('awaitingTarget', 'dismissed') AND num_nonnulls(target_machine_id, target_machine_draft_id) <= 1) "
        + "OR (state NOT IN ('awaitingTarget', 'dismissed') "
        + "AND num_nonnulls(target_machine_id, target_machine_draft_id) = 1)";

    public const string RangeSql = "min IS NULL OR max IS NULL OR min < max";

    public const string WarningLowSql = "warning_low IS NULL OR alarm_low IS NULL OR warning_low >= alarm_low";

    public const string WarningHighSql = "warning_high IS NULL OR alarm_high IS NULL OR warning_high <= alarm_high";

    public const string PhaseConditionSourceSql =
        "num_nonnulls(metric_definition_id, metric_draft_id, status_definition_id, status_draft_id) = 1";

    public const string PhaseLimitMetricSql = "num_nonnulls(metric_definition_id, metric_draft_id) = 1";

    public const string EntryMachineSql = "(machine_id IS NOT NULL) = (state = 'onMachine')";

    public const string ProposalSourceFieldsSql =
        "(source = 'ingest' AND attempt_id IS NOT NULL AND file_id IS NOT NULL "
        + "AND chat_id IS NULL AND chat_message_id IS NULL) "
        + "OR (source = 'chat' AND chat_id IS NOT NULL AND chat_message_id IS NOT NULL "
        + "AND attempt_id IS NULL AND file_id IS NULL)";
}
