using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Infrastructure.DefinitionDrafts.Persistence;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;

/// <summary>The ids of the rows that one constraint test stores.</summary>
public sealed record StoredDraftRows
{
    public required Guid MetricId { get; init; }

    public required Guid StatusId { get; init; }

    public required Guid EntryId { get; init; }
}

/// <summary>An update that a check constraint must reject, and the name of that constraint.</summary>
public sealed record InvalidUpdateCase
{
    public required string Sql { get; init; }

    public required Func<StoredDraftRows, Guid> RowId { get; init; }

    public required string ConstraintName { get; init; }
}

public static class DefinitionDraftTestData
{
    public static readonly Guid CompanyId = Guid.Parse(input: "0f1a2b3c-1000-7000-8000-000000000001");
    public static readonly Guid AttemptId = Guid.Parse(input: "0f1a2b3c-1000-7000-8000-000000000002");
    public static readonly Guid FileId = Guid.Parse(input: "0f1a2b3c-1000-7000-8000-000000000003");
    public static readonly Guid MachineId = Guid.Parse(input: "0f1a2b3c-1000-7000-8000-000000000004");
    public static readonly Guid MachineDraftId = Guid.Parse(input: "0f1a2b3c-1000-7000-8000-000000000005");

    public static TheoryData<string, InvalidUpdateCase> InvalidUpdates => new()
    {
        {
            "pending metric draft without a target",
            new InvalidUpdateCase
            {
                Sql = "UPDATE metric_definition_drafts SET state = 'pending' WHERE id = {0}",
                RowId = rows => rows.MetricId,
                ConstraintName = DefinitionDraftCheckNames.MetricTarget,
            }
        },
        {
            "pending status draft without a target",
            new InvalidUpdateCase
            {
                Sql = "UPDATE status_definition_drafts SET state = 'pending' WHERE id = {0}",
                RowId = rows => rows.StatusId,
                ConstraintName = DefinitionDraftCheckNames.StatusTarget,
            }
        },
        {
            "metric draft with both targets",
            new InvalidUpdateCase
            {
                Sql = "UPDATE metric_definition_drafts SET target_machine_id = '"
                    + MachineId
                    + "', target_machine_draft_id = '"
                    + MachineDraftId
                    + "' WHERE id = {0}",
                RowId = rows => rows.MetricId,
                ConstraintName = DefinitionDraftCheckNames.MetricTarget,
            }
        },
        {
            "status draft with both targets",
            new InvalidUpdateCase
            {
                Sql = "UPDATE status_definition_drafts SET target_machine_id = '"
                    + MachineId
                    + "', target_machine_draft_id = '"
                    + MachineDraftId
                    + "' WHERE id = {0}",
                RowId = rows => rows.StatusId,
                ConstraintName = DefinitionDraftCheckNames.StatusTarget,
            }
        },
        {
            "entry onMachine without a machine",
            new InvalidUpdateCase
            {
                Sql = "UPDATE proposal_entries SET state = 'onMachine' WHERE id = {0}",
                RowId = rows => rows.EntryId,
                ConstraintName = DefinitionDraftCheckNames.EntryMachine,
            }
        },
    };

    public static MetricDefinitionDraft CreateMetric(
        Guid id,
        Guid entryId,
        Guid? tentativeMachineId = null,
        Guid? companyId = null
    ) =>
        MetricDefinitionDraft.Propose(
            id,
            companyId ?? CompanyId,
            AttemptId,
            FileId,
            entryId,
            tentativeMachineId,
            new MetricDraftValues { Key = "spindle.temperature", Name = "Spindle temperature" });

    public static StatusDefinitionDraft CreateStatus(
        Guid id,
        Guid entryId,
        Guid? tentativeMachineId = null,
        Guid? companyId = null
    ) =>
        StatusDefinitionDraft.Propose(
            id,
            companyId ?? CompanyId,
            AttemptId,
            FileId,
            entryId,
            tentativeMachineId,
            new StatusDraftValues { Code = "E1" });
}
