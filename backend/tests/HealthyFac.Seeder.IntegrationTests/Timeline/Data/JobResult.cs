namespace HealthyFac.Seeder.IntegrationTests.Timeline.Data;

public sealed record JobResult
{
    public required int ExitCode { get; init; }

    public required string Output { get; init; }
}
