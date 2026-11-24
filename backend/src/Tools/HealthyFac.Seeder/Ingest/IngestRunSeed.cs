namespace HealthyFac.Seeder.Ingest;

public sealed class IngestRunSeed
{
    public required Guid Id { get; init; }

    public required string Role { get; init; }

    public required string Model { get; init; }

    public required string PromptVersion { get; init; }

    public required string PromptHash { get; init; }

    public required TimeSpan DueAfter { get; init; }

    public required TimeSpan StartedAfter { get; init; }

    public required TimeSpan FinishedAfter { get; init; }

    public required int InputTokens { get; init; }

    public required int OutputTokens { get; init; }

    public required IReadOnlyList<IngestToolCallSeed> ToolCalls { get; init; }
}
