namespace HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;

/// <summary>
/// Holds the number of connected MQTT consumers for the health check. The consumers write it, the check reads it.
/// </summary>
public sealed class MqttIntakeState
{
    private int _connectedCount;

    public int ConnectedCount => Volatile.Read(ref _connectedCount);

    public bool IsConnected => ConnectedCount > 0;

    public void MarkConnected() => Interlocked.Increment(ref _connectedCount);

    public void MarkDisconnected() => Interlocked.Decrement(ref _connectedCount);
}
