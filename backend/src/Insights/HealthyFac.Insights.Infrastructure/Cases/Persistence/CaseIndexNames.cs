namespace HealthyFac.Insights.Infrastructure.Cases.Persistence;

/// <summary>
/// Defines the names of the indexes of the case tables that code reads, so a name never differs between the mapping
/// and a check.
/// </summary>
public static class CaseIndexNames
{
    public const string OpenMachine = "ux_maintenance_cases_open_machine";
    public const string SignalCase = "ix_machine_signals_case_id";
    public const string SignalMachineState = "ix_machine_signals_company_id_machine_id_state";
}
