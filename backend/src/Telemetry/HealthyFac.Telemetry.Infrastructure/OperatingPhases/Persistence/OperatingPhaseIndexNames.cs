namespace HealthyFac.Telemetry.Infrastructure.OperatingPhases.Persistence;

/// <summary>
/// Defines the database index names of the operating phase tables that code reads, so a name never differs between
/// the mapping and a check.
/// </summary>
public static class OperatingPhaseIndexNames
{
    public const string MachineCode = "ix_operating_phases_machine_registration_id_code";
    public const string MachinePriority = "ix_operating_phases_machine_registration_id_priority";
}
