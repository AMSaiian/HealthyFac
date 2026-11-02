namespace HealthyFac.Shared.Infrastructure.CurrentUser;

/// <summary>
/// Defines the headers that carry the actor of a flow from one service to another. A message and a service call use
/// the same headers, and the receiver restores the actor with the same rule. Neither carries a token for the actor.
/// </summary>
public static class ActorHeaderNames
{
    public const string ActorId = "healthyfac-actor-id";
    public const string CompanyId = "healthyfac-company-id";
}
