using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Messaging.Machines;

/// <summary>
/// Lists the state values of the machine messages. The values equal the domain machine state, because Core Messaging
/// cannot reference Core Domain. Never rename a value in place: stored outbox and inbox rows use it.
/// </summary>
public sealed class MachineStateValue : Enumeration<MachineStateValue>
{
    public static readonly MachineStateValue Operational = new(value: "operational");
    public static readonly MachineStateValue InMaintenance = new(value: "inMaintenance");
    public static readonly MachineStateValue OutOfService = new(value: "outOfService");

    private MachineStateValue(string value)
        : base(value)
    {
    }
}
