using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Telemetry.Domain.MachineRegistrations;

/// <summary>
/// Lists the states of a machine that Telemetry follows. The values equal the Core machine state values of the
/// <c>MachineStateChanged</c> message.
/// </summary>
public sealed class MachineRegistrationState : Enumeration<MachineRegistrationState>
{
    public static readonly MachineRegistrationState Operational = new(value: "operational");
    public static readonly MachineRegistrationState InMaintenance = new(value: "inMaintenance");
    public static readonly MachineRegistrationState OutOfService = new(value: "outOfService");

    private MachineRegistrationState(string value)
        : base(value)
    {
    }
}
