namespace HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;

/// <summary>
/// Holds the number of connected MQTT consumers for the health check and the number of messages in flight. The
/// consumers write it. The health check and the intake gauges read it.
/// </summary>
public sealed class MqttIntakeState
{
    private int _connectedCount;
    private int _inFlightCount;

    public int ConnectedCount => Volatile.Read(ref _connectedCount);

    public int InFlightCount => Volatile.Read(ref _inFlightCount);

    public bool IsConnected => ConnectedCount > 0;

    public void MarkConnected() => Interlocked.Increment(ref _connectedCount);

    public void MarkDisconnected() => Interlocked.Decrement(ref _connectedCount);

    public void MarkMessageStarted() => Interlocked.Increment(ref _inFlightCount);

    public void MarkMessageEnded() => Interlocked.Decrement(ref _inFlightCount);
}
