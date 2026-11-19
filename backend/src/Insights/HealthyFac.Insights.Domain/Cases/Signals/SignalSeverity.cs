using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Cases.Signals;

/// <summary>
/// Lists the severities of a machine signal. The values equal the signal severity of Telemetry. Never rename a value
/// in place: stored rows use it.
/// </summary>
public sealed class SignalSeverity : Enumeration<SignalSeverity>
{
    public static readonly SignalSeverity Warning = new(value: "warning");
    public static readonly SignalSeverity Alarm = new(value: "alarm");

    private SignalSeverity(string value)
        : base(value)
    {
    }
}
