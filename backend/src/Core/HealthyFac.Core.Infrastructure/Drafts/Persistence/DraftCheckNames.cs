namespace HealthyFac.Core.Infrastructure.Drafts.Persistence;

/// <summary>
/// Defines the names of the check constraints that the shared helpers cannot build, so a test reads the same name as
/// the mapping.
/// </summary>
public static class DraftCheckNames
{
    public const string MetricOneTarget = "ck_metric_definition_draft_one_target";
    public const string StatusOneTarget = "ck_status_definition_draft_one_target";
    public const string MetricRange = "ck_metric_definition_draft_min_below_max";
    public const string MetricValueKind = "ck_metric_definition_draft_value_kind_allowed";
    public const string StatusSeverity = "ck_status_definition_draft_severity_allowed";

    public const string OneTargetSql = "num_nonnulls(target_machine_id, target_machine_draft_id) = 1";
    public const string RangeSql = "min IS NULL OR max IS NULL OR min < max";
}
