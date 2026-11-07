using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;

/// <summary>
/// Reports <see cref="HealthStatus.Degraded"/> while the MQTT intake is enabled and fewer consumers than
/// <see cref="MqttIntakeOptions.ConsumerCount"/> are connected. The service still serves its API without the broker,
/// so the status is never unhealthy. The check reads the state of the consumers and opens no connection, so it needs
/// no timeout of its own.
/// </summary>
/// <param name="options">The consumer settings.</param>
/// <param name="state">The connection state.</param>
public sealed class MqttIntakeHealthCheck(IOptions<MqttIntakeOptions> options, MqttIntakeState state) : IHealthCheck
{
    private readonly MqttIntakeOptions _options = options.Value;
    private readonly MqttIntakeState _state = state;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var result = !_options.IsEnabled || _state.ConnectedCount >= _options.ConsumerCount
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Degraded();

        return Task.FromResult(result);
    }
}
