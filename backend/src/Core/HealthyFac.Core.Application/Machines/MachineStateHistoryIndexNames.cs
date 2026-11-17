namespace HealthyFac.Core.Application.Machines;

/// <summary>
/// Names the unique index of the machine state history. The handlers read it to tell a same-instant parallel state
/// change from another unique violation. It sits in Application, because the handlers read it and Infrastructure
/// references Application.
/// </summary>
public static class MachineStateHistoryIndexNames
{
    public const string MachineStartedAt = "IX_machine_state_histories_machine_id_started_at";
}
