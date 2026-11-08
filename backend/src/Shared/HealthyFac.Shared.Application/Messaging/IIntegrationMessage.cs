namespace HealthyFac.Shared.Application.Messaging;

/// <summary>
/// Marks a message that one service sends to another service through the broker. The contract lives in the
/// Messaging project of the producer, and the consumer references it. A contract that a service consumes also
/// implements <c>IRequest</c>, so the use case pipeline handles it. The
/// inbox processor calls the use case once per message, inside one database transaction. The use case runs as the
/// original actor of the message, never checks permissions again, and must keep a payload company equal to the
/// actor company unless the actor is the system actor.
/// </summary>
public interface IIntegrationMessage
{
    /// <summary>
    /// The stable contract name, for example <c>core.v1:EmployeeProvisioningRequested</c>. The outbox stores this
    /// name instead of the CLR type name, so a class rename does not break stored messages, and the receiving
    /// service resolves the payload type by this name.
    /// </summary>
    static abstract string MessageType { get; }
}
