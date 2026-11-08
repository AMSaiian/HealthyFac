using System.Diagnostics.Metrics;
using HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.Intake.Data;

/// <summary>
/// A consumer that a stop or start test created, with its connection state. The test disposes it after it stops the
/// consumer, which disposes the meters of the consumer.
/// </summary>
public sealed class StartedIntake : IAsyncDisposable
{
    /// <summary>The consumer service. The test starts and stops it.</summary>
    public required MqttIntakeService Service { get; init; }

    /// <summary>The connection state of the service.</summary>
    public required MqttIntakeState State { get; init; }

    /// <summary>
    /// The services that hold the meter factory of this consumer. <see cref="DisposeAsync"/> disposes them.
    /// </summary>
    public required ServiceProvider MeterServices { get; init; }

    /// <summary>
    /// The meter factory that only this consumer uses. A <c>MetricCollector</c> on it reads the measurements of this
    /// consumer and no other, also when other test collections run at the same time.
    /// </summary>
    public IMeterFactory MeterFactory => MeterServices.GetRequiredService<IMeterFactory>();

    public async ValueTask DisposeAsync() => await MeterServices.DisposeAsync();
}
