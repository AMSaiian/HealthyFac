using HealthyFac.Telemetry.Infrastructure.DefinitionDrafts.Persistence;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;

public static class PhaseDraftConstraintTestData
{
    private const string ConditionInsert =
        "INSERT INTO operating_phase_draft_conditions "
        + "(id, draft_id, metric_definition_id, metric_draft_id, status_definition_id, status_draft_id, created_at, "
        + "created_by) VALUES (gen_random_uuid(), {0}, ";

    private const string LimitInsert =
        "INSERT INTO operating_phase_draft_limits (id, draft_id, metric_definition_id, metric_draft_id, created_at, "
        + "created_by) VALUES (gen_random_uuid(), {0}, ";

    private const string ConditionTail = ", now(), gen_random_uuid())";

    public static TheoryData<string, string, string> InvalidRows => new()
    {
        {
            "a phase draft with an empty proposal entry",
            "UPDATE operating_phase_drafts SET proposal_entry_id = "
            + DefinitionDraftTestData.EmptyGuidSql
            + " WHERE id = {0}",
            DefinitionDraftTestData.PhaseEntryNotEmpty
        },
        {
            "a condition with a metric definition and a metric draft",
            ConditionInsert + "gen_random_uuid(), gen_random_uuid(), NULL, NULL" + ConditionTail,
            DefinitionDraftCheckNames.PhaseConditionSource
        },
        {
            "a condition with a metric and a status",
            ConditionInsert + "gen_random_uuid(), NULL, gen_random_uuid(), NULL" + ConditionTail,
            DefinitionDraftCheckNames.PhaseConditionSource
        },
        {
            "a condition without a source",
            ConditionInsert + "NULL, NULL, NULL, NULL" + ConditionTail,
            DefinitionDraftCheckNames.PhaseConditionSource
        },
        {
            "a limit row with a metric definition and a metric draft",
            LimitInsert + "gen_random_uuid(), gen_random_uuid()" + ConditionTail,
            DefinitionDraftCheckNames.PhaseLimitMetric
        },
        {
            "a limit row without a metric",
            LimitInsert + "NULL, NULL" + ConditionTail,
            DefinitionDraftCheckNames.PhaseLimitMetric
        },
    };

    public static TheoryData<string, string> ValidRows => new()
    {
        {
            "a phase draft without a proposal entry",
            "UPDATE operating_phase_drafts SET proposal_entry_id = NULL WHERE id = {0}"
        },
        { "a condition on a status draft", ConditionInsert + "NULL, NULL, NULL, gen_random_uuid()" + ConditionTail },
        {
            "a condition on a metric definition",
            ConditionInsert + "gen_random_uuid(), NULL, NULL, NULL" + ConditionTail
        },
        { "a limit row on a metric draft", LimitInsert + "NULL, gen_random_uuid()" + ConditionTail },
    };
}
