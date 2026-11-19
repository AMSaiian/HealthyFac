namespace HealthyFac.Telemetry.Domain.SignalEpisodes;

/// <summary>
/// Defines the exceptions that a signal episode throws when a caller breaks an invariant. The evaluation checks the
/// state first, so these exceptions mark a programming error.
/// </summary>
public static class SignalEpisodeInvariantErrors
{
    public static InvalidOperationException EscalateOnlyFromOpenWarning() =>
        new(message: "Only an open episode with the severity warning escalates.");

    public static InvalidOperationException ResolveOnlyOnce() =>
        new(message: "Only an open episode resolves.");
}
