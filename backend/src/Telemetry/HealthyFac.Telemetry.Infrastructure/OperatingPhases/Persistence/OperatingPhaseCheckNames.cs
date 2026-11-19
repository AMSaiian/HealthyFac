namespace HealthyFac.Telemetry.Infrastructure.OperatingPhases.Persistence;

/// <summary>
/// Defines the check constraint names of the operating phase child tables and their SQL, so a test reads the name
/// of the constraint that rejects a row.
/// </summary>
public static class OperatingPhaseCheckNames
{
    public const string ConditionExactlyOneSource = "ck_operating_phase_condition_exactly_one_source";

    public const string ConditionExactlyOneSourceSql =
        "(metric_definition_id IS NOT NULL) <> (status_definition_id IS NOT NULL)";

    public const string ConditionOperatorAllowed = "ck_operating_phase_condition_operator_allowed";
}
