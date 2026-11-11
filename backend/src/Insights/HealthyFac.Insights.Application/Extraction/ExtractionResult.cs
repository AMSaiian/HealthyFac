namespace HealthyFac.Insights.Application.Extraction;

/// <summary>
/// The structured answer of the model: the machines that a document describes. It is also the response schema of the
/// model call.
/// </summary>
public record ExtractionResult
{
    public IReadOnlyList<ExtractedMachine> Machines { get; init; } = [];
}
