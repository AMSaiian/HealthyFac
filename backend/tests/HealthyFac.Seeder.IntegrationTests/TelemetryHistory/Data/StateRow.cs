namespace HealthyFac.Seeder.IntegrationTests.TelemetryHistory.Data;

public sealed class StateRow
{
    public required string State { get; init; }

    public required DateTime StartedAt { get; init; }
}
