using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Telemetry.Messaging.Signals;

/// <summary>
/// Lists the severity values of the signal messages. The values equal the domain signal severity. Never rename a
/// value in place: stored outbox and inbox rows use it.
/// </summary>
public sealed class SignalSeverityValue : Enumeration<SignalSeverityValue>
{
    public static readonly SignalSeverityValue Warning = new(value: "warning");
    public static readonly SignalSeverityValue Alarm = new(value: "alarm");

    private SignalSeverityValue(string value)
        : base(value)
    {
    }
}
