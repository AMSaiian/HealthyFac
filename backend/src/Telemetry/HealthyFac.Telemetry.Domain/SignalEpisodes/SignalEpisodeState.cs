using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Telemetry.Domain.SignalEpisodes;

/// <summary>
/// Defines the state of a signal episode. The camelCase values reach the database and the messages, so never rename
/// one in place.
/// </summary>
public sealed class SignalEpisodeState : Enumeration<SignalEpisodeState>
{
    public static readonly SignalEpisodeState Open = new(value: "open");
    public static readonly SignalEpisodeState Resolved = new(value: "resolved");

    private SignalEpisodeState(string value)
        : base(value)
    {
    }
}
