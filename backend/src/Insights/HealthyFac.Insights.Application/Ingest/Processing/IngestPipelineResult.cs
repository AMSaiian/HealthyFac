using HealthyFac.Insights.Application.Conversion;
using HealthyFac.Insights.Application.Extraction;

namespace HealthyFac.Insights.Application.Ingest.Processing;

/// <summary>
/// What the pipeline produced: the file facts, the chunks with their embeddings, and the mapped drafts.
/// </summary>
public record IngestPipelineResult
{
    public Guid? MachineId { get; init; }

    public required DateTime MachineChangedAt { get; init; }

    public required int PageCount { get; init; }

    public required IReadOnlyList<ConvertedChunk> Chunks { get; init; }

    public required IReadOnlyList<float[]> Embeddings { get; init; }

    public required MappingResult Mapped { get; init; }
}
