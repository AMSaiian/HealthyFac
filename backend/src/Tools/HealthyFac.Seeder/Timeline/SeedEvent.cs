using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.Timeline;

/// <summary>
/// The envelope of one seed event. A part lists its events in file order. The job runs the events in the order of
/// their moments, and the clock reads the moment of the running event.
/// </summary>
public sealed class SeedEvent
{
    public required RelativeMoment At { get; init; }

    /// <summary>The key of the person who acts, or <see langword="null"/> for the system actor.</summary>
    public string? Actor { get; init; }

    /// <summary>The file and the key of the item, built with <see cref="SeedItem.Of"/>.</summary>
    public required string Item { get; init; }

    /// <summary>The keys that the event creates (e.g., the ID of a new machine).</summary>
    public IReadOnlyList<string> Creates { get; init; } = [];

    /// <summary>The keys that the event reads. Another event must create each key before this event.</summary>
    public IReadOnlyList<string> Uses { get; init; } = [];

    /// <summary>The requests that the event sends, for the check of their validators.</summary>
    public IReadOnlyList<SeedRequest> Requests { get; init; } = [];

    /// <summary>The credentials that the event sends to Keycloak, for the check of the realm policy.</summary>
    public IReadOnlyList<SeedCredential> Credentials { get; init; } = [];
}
