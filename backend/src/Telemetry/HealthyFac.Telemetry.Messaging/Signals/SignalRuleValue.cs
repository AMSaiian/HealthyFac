using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Telemetry.Messaging.Signals;

/// <summary>
/// Lists the rule values of the signal messages. The values equal the domain signal rule, because Telemetry Messaging
/// cannot reference Telemetry Domain. Never rename a value in place: stored outbox and inbox rows use it.
/// </summary>
public sealed class SignalRuleValue : Enumeration<SignalRuleValue>
{
    public static readonly SignalRuleValue Threshold = new(value: "threshold");
    public static readonly SignalRuleValue Drift = new(value: "drift");
    public static readonly SignalRuleValue Trend = new(value: "trend");
    public static readonly SignalRuleValue Status = new(value: "status");
    public static readonly SignalRuleValue SilentMachine = new(value: "silentMachine");
    public static readonly SignalRuleValue UnknownMode = new(value: "unknownMode");

    private SignalRuleValue(string value)
        : base(value)
    {
    }
}
