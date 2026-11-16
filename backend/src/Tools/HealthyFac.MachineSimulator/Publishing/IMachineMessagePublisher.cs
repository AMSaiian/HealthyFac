namespace HealthyFac.MachineSimulator.Publishing;

/// <summary>
/// Sends one machine message to the broker. The simulator keeps no buffer: a message that the broker does not take is
/// dropped.
/// </summary>
public interface IMachineMessagePublisher
{
    /// <summary>
    /// Publishes a payload to the topic of a gateway.
    /// </summary>
    /// <param name="gatewayId">The gateway ID of the machine.</param>
    /// <param name="payload">The JSON payload.</param>
    /// <param name="cancellationToken">Cancels the publish.</param>
    /// <returns><see langword="true"/> when the broker took the message.</returns>
    Task<bool> TryPublishAsync(string gatewayId, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken);
}
