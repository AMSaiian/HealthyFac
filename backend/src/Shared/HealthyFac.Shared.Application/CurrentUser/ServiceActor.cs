using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Shared.Application.CurrentUser;

/// <summary>
/// Defines the flows that shared code runs in every service: the database start-up and the two message processors.
/// A flow of one service is declared in the project of that service with <see cref="KnownActors.ForServiceFlow"/>
/// and a camelCase flow name, never in this enumeration. The camelCase values are part of the actor IDs, so never
/// rename one in place.
/// </summary>
public sealed class ServiceActor : Enumeration<ServiceActor>
{
    public static readonly ServiceActor DatabaseInitialization = new("databaseInitialization");
    public static readonly ServiceActor OutboxProcessor = new("outboxProcessor");
    public static readonly ServiceActor InboxProcessor = new("inboxProcessor");

    private ServiceActor(string value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates the actor of this flow in a service.
    /// </summary>
    /// <param name="service">The service that runs the flow.</param>
    /// <param name="companyId">The company of a flow that runs for one tenant, or <see langword="null"/>.</param>
    /// <returns>An <see cref="ActorContext"/> of the kind <see cref="ActorKind.Service"/>.</returns>
    public ActorContext For(ServiceName service, Guid? companyId = null) =>
        KnownActors.ForServiceFlow(service, Value, companyId);
}
