using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Telemetry.Domain.SignalEpisodes;

/// <summary>
/// Defines the rules that open a signal episode. The camelCase values reach the database and the messages, so never
/// rename one in place.
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
