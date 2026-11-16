using HealthyFac.MachineSimulator.Publishing;

namespace HealthyFac.MachineSimulator.UnitTests.Simulation.Data;

/// <summary>
/// Records the published messages. A test switches <see cref="IsAccepting"/> off to simulate a broker that is down.
/// </summary>
public sealed class FakeMachineMessagePublisher : IMachineMessagePublisher
{
    public bool IsAccepting { get; set; } = true;

    public int FailuresLeft { get; set; }

    public List<(string GatewayId, byte[] Payload)> Messages { get; } = [];

    public Task<bool> TryPublishAsync(
        string gatewayId,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken
    )
    {
        if (FailuresLeft > 0)
        {
            FailuresLeft--;

            throw new InvalidOperationException(message: "The publisher failed once.");
        }

        if (IsAccepting)
        {
            Messages.Add((gatewayId, payload.ToArray()));
        }

        return Task.FromResult(IsAccepting);
    }
}
