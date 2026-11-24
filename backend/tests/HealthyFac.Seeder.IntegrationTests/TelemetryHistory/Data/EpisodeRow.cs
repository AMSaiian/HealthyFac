namespace HealthyFac.Seeder.IntegrationTests.TelemetryHistory.Data;

public sealed class EpisodeRow
{
    public required Guid MachineId { get; init; }

    public required string Rule { get; init; }

    public string? MetricKey { get; init; }

    public required string Severity { get; init; }

    public required string State { get; init; }

    public required DateTime OpenedAt { get; init; }

    public DateTime? ResolvedAt { get; init; }
}
