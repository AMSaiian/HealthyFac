using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthyFac.MachineSimulator.Publishing;

/// <summary>
/// Reports <see cref="HealthStatus.Unhealthy"/> while the MQTT client is not connected. The simulator has no work
/// without the broker, so the status is unhealthy. The check reads the state of the client and opens no connection.
/// </summary>
/// <param name="publisher">The publisher that owns the client.</param>
public sealed class MqttConnectionHealthCheck(MqttMachineMessagePublisher publisher) : IHealthCheck
{
    public const string Name = "mqtt-connection";

    private readonly MqttMachineMessagePublisher _publisher = publisher;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        Task.FromResult(_publisher.IsConnected ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy());
}
