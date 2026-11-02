namespace HealthyFac.Shared.Infrastructure.Messaging.Transport;

/// <summary>
/// Defines the message headers that carry the trace context across services. The actor headers are in
/// <see cref="CurrentUser.ActorHeaderNames"/>. A message never carries a token.
/// </summary>
public static class MessageHeaders
{
    public const string TraceParent = "traceparent";
}
