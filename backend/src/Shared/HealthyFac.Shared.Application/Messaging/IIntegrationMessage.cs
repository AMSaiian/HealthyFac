namespace HealthyFac.Shared.Application.Messaging;

/// <summary>
/// Marks a message that one service sends to another service through the broker.
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
