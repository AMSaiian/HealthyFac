using HealthyFac.Shared.Application.CurrentUser.Actors;

namespace HealthyFac.Seeder.Timeline;

/// <summary>
/// The state that the events of a run share: the anchor, the company zone, the IDs that events created (by key),
/// and the actors of the people.
/// </summary>
public sealed class SeedContext
{
    private readonly Dictionary<string, Guid> _ids = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ActorContext> _actors = new(StringComparer.Ordinal);

    public required DateTimeOffset Anchor { get; init; }

    public required TimeZoneInfo Zone { get; init; }

    /// <summary>Stores the ID that an event created.</summary>
    /// <param name="key">The key of the item.</param>
    /// <param name="id">The ID.</param>
    public void SetId(string key, Guid id) => _ids[key] = id;

    /// <summary>Reads an ID that an earlier event created.</summary>
    /// <param name="key">The key of the item.</param>
    /// <returns>The ID.</returns>
    /// <exception cref="InvalidOperationException">No event created the key.</exception>
    public Guid GetId(string key) =>
        _ids.TryGetValue(key, out var id) ? id : throw new InvalidOperationException(TimelineErrors.KeyUnknown(key));

    public void SetActor(string person, ActorContext actor) => _actors[person] = actor;

    /// <summary>Reads the actor of a person.</summary>
    /// <param name="person">The key of the person.</param>
    /// <returns>The actor.</returns>
    /// <exception cref="InvalidOperationException">The person has no actor.</exception>
    public ActorContext GetActor(string person) =>
        _actors.TryGetValue(person, out var actor)
            ? actor
            : throw new InvalidOperationException(TimelineErrors.ActorUnknown(person));
}
