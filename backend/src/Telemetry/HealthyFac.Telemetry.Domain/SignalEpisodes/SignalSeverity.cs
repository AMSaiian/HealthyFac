using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Telemetry.Domain.SignalEpisodes;

/// <summary>
/// Defines the severity of a signal episode. The camelCase values reach the database and the messages, so never
/// rename one in place.
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
