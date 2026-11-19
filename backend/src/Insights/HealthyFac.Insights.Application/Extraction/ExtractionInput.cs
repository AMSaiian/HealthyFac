using HealthyFac.Insights.Application.Conversion;
using HealthyFac.Insights.Application.Extraction.Signals;
using HealthyFac.Insights.Application.MetricUnits;

namespace HealthyFac.Insights.Application.Extraction;

/// <summary>
/// What the model sees: the chunks of the document, the unit catalog, and the metrics and statuses that the target
/// machine already has.
/// </summary>
public record ExtractionInput
{
    public required Guid CompanyId { get; init; }

    public required IReadOnlyList<ConvertedChunk> Chunks { get; init; }

    public required IReadOnlyList<MetricUnit> Units { get; init; }

    public required MachineSignals Signals { get; init; }

    /// <summary>Tells whether the document belongs to one machine. A company file belongs to none.</summary>
    public required bool IsMachineFile { get; init; }
}
