namespace HealthyFac.Shared.Infrastructure.Messaging.Transport;

/// <summary>
/// The only message type that Rebus carries. The envelope holds the contract name and the JSON that the outbox
/// stored, so Rebus never needs the CLR type of a payload and the receiver resolves the payload type itself.
/// </summary>
public sealed record IntegrationEnvelope
{
    public required string Type { get; init; }

    public required string Payload { get; init; }
}
