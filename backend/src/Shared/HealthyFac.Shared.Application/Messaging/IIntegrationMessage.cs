namespace HealthyFac.Shared.Application.Messaging;

/// <summary>
/// Marks a message that one service sends to another service through the broker. A service that consumes the
/// message also implements <c>IRequest</c> on its copy of the contract, so the use case pipeline handles it. The
/// inbox processor calls the use case once per message, inside one database transaction. The use case runs as the
/// original actor of the message, never checks permissions again, and must keep a payload company equal to the
/// actor company unless the actor is the system actor.
/// </summary>
public interface IIntegrationMessage
{
    /// <summary>
    /// The stable contract name, for example <c>core.v1:EmployeeProvisioningRequested</c>. The outbox stores this
    /// name instead of the CLR type name, so a class rename does not break stored messages, and the receiving
    /// service resolves the payload type without a reference to the sender assembly.
    /// </summary>
    static abstract string MessageType { get; }
}
