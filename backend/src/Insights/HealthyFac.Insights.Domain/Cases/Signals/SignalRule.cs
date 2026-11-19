using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Cases.Signals;

/// <summary>
/// Lists the rules of a machine signal. The values equal the signal rule of Telemetry, because the signal comes from
/// the <c>SignalEpisodeChanged</c> message. Never rename a value in place: stored rows use it.
/// </summary>
public sealed class SignalRule : Enumeration<SignalRule>
{
    public static readonly SignalRule Threshold = new(value: "threshold");
    public static readonly SignalRule Drift = new(value: "drift");
    public static readonly SignalRule Trend = new(value: "trend");
    public static readonly SignalRule Status = new(value: "status");
    public static readonly SignalRule SilentMachine = new(value: "silentMachine");
    public static readonly SignalRule UnknownMode = new(value: "unknownMode");

    private SignalRule(string value)
        : base(value)
    {
    }
}
