using System.Collections.Frozen;
using UUIDNext;

namespace HealthyFac.Shared.Application.CurrentUser;

/// <summary>
/// Defines the actors that are not users and tells their kind from their IDs. Every actor ID is a name-based UUID
/// (version 5) of its key in <see cref="Namespace"/>: <c>anonymous</c>, or <c>service:&lt;service&gt;:&lt;flow&gt;</c>.
/// The ID is the same in every environment and needs no table. A Keycloak user ID is version 4, so it never equals
/// an actor ID.
/// </summary>
public static class KnownActors
{
    /// <summary>
    /// The namespace of every actor ID. Never change it, because every actor ID in the audit columns and the
    /// messages depends on it.
    /// </summary>
    public static readonly Guid Namespace = Guid.Parse(input: "aae8d67c-0d95-40ea-bda9-142732768543");

    private const int NameBasedVersion = 5;

    public static ActorContext Anonymous { get; } = new()
    {
        Kind = ActorKind.Anonymous,
        UserId = IdOf(ActorKind.Anonymous.Value),
        DepartmentIds = FrozenSet<Guid>.Empty,
    };

    /// <summary>
    /// Creates the ID of an actor from its key. Every actor ID comes from this method.
    /// </summary>
    /// <param name="key">The key of the actor.</param>
    /// <returns>The name-based UUID of <paramref name="key"/> in <see cref="Namespace"/>.</returns>
    public static Guid IdOf(string key) => Uuid.NewNameBased(Namespace, key);

    /// <summary>
    /// Tells the kind of an actor from its ID alone. This is the one definition of the rule. A receiver uses it to
    /// restore an actor, and an audit view uses it to choose a generic label for an actor that is not a user.
    /// </summary>
    /// <param name="actorId">The actor ID.</param>
    /// <returns><see cref="ActorKind.Anonymous"/> for the anonymous ID, <see cref="ActorKind.Service"/> for any other
    /// version 5 ID (a service of any version creates it), and <see cref="ActorKind.User"/> for every other
    /// ID.</returns>
    public static ActorKind KindOf(Guid actorId)
    {
        if (actorId == Anonymous.UserId)
        {
            return ActorKind.Anonymous;
        }

        return actorId.Version == NameBasedVersion ? ActorKind.Service : ActorKind.User;
    }

    /// <summary>
    /// Creates the actor of a flow of a service. This is the only builder of a service actor. A flow of one service
    /// is declared in the project of that service with this method and a camelCase flow name. The common flows that
    /// shared code runs are in <see cref="ServiceActor"/>.
    /// </summary>
    /// <param name="service">The service that runs the flow.</param>
    /// <param name="flow">The camelCase name of the flow. It is part of the actor ID, so never rename it.</param>
    /// <param name="companyId">The company of a flow that runs for one tenant, or <see langword="null"/>.</param>
    /// <returns>An <see cref="ActorContext"/> of the kind <see cref="ActorKind.Service"/> with the name
    /// <c>&lt;service&gt;:&lt;flow&gt;</c>.</returns>
    public static ActorContext ForServiceFlow(ServiceName service, string flow, Guid? companyId = null) =>
        new()
        {
            Kind = ActorKind.Service,
            Name = $"{service.Value}:{flow}",
            UserId = IdOf($"{ActorKind.Service.Value}:{service.Value}:{flow}"),
            CompanyId = companyId,
            DepartmentIds = FrozenSet<Guid>.Empty,
        };

    /// <summary>
    /// Restores the actor of a received message from the ID and the company that the message headers hold. The
    /// receiver keeps the ID as it is and restores no name, because only the service that runs a flow knows its name.
    /// </summary>
    /// <param name="actorId">The actor ID from the message.</param>
    /// <param name="companyId">The company from the message, or <see langword="null"/>.</param>
    /// <returns>An actor with the kind from <see cref="KindOf"/>, no name, and the company.</returns>
    public static ActorContext Restore(Guid actorId, Guid? companyId) =>
        new()
        {
            Kind = KindOf(actorId),
            UserId = actorId,
            CompanyId = companyId,
            DepartmentIds = FrozenSet<Guid>.Empty,
        };
}
