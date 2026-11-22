using System.Text.Json;

namespace HealthyFac.Seeder.Ingest;

public sealed class IngestToolCallSeed
{
    public required Guid Id { get; init; }

    public required int Sequence { get; init; }

    public required string CallId { get; init; }

    public required string ToolName { get; init; }

    public required JsonElement Arguments { get; init; }

    public required string Outcome { get; init; }

    public required JsonElement Result { get; init; }

    public required int DurationMilliseconds { get; init; }
}
