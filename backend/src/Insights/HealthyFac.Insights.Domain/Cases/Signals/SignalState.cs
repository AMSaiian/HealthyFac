using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Cases.Signals;

/// <summary>
/// Lists the states of a machine signal. The values equal the episode state of Telemetry. Never rename a value in
/// place: stored rows use it.
/// </summary>
public sealed class SignalState : Enumeration<SignalState>
{
    public static readonly SignalState Open = new(value: "open");
    public static readonly SignalState Resolved = new(value: "resolved");

    private SignalState(string value)
        : base(value)
    {
    }
}
