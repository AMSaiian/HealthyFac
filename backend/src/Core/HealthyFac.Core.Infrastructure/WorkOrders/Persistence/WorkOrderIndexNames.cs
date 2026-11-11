namespace HealthyFac.Core.Infrastructure.WorkOrders.Persistence;

/// <summary>
/// Defines the database key and index names of the work order tables that code reads, so a name never differs between
/// the mapping and a check.
/// </summary>
public static class WorkOrderIndexNames
{
    public const string HistoryRevision = "ak_work_order_histories_work_order_id_revision";
    public const string HistoryAssigneePlannedStart = "ix_work_order_histories_assignee_id_planned_start_utc";
    public const string FileLink = "ix_work_order_files_work_order_id_file_id";
}
