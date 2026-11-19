using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Telemetry.Messaging.Signals;

/// <summary>
/// Lists the state values of the signal messages. The values equal the domain episode state. Never rename a value in
/// place: stored outbox and inbox rows use it.
/// </summary>
public sealed class SignalEpisodeStateValue : Enumeration<SignalEpisodeStateValue>
{
    public static readonly SignalEpisodeStateValue Open = new(value: "open");
    public static readonly SignalEpisodeStateValue Resolved = new(value: "resolved");

    private SignalEpisodeStateValue(string value)
        : base(value)
    {
    }
}
