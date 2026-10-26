namespace HealthyFac.Shared.Infrastructure.Messaging.Transport;

/// <summary>
/// Defines the message headers that carry the actor and the trace context across services. A message never
/// carries a token.
/// </summary>
public static class MessageHeaders
{
    public const string ActorId = "healthyfac-actor-id";
    public const string CompanyId = "healthyfac-company-id";
    public const string TraceParent = "traceparent";
}
