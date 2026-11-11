using HealthyFac.Insights.Application.Conversion;
using HealthyFac.Insights.Application.MetricUnits;

namespace HealthyFac.Insights.Application.Extraction;

/// <summary>
/// What the model sees: the chunks of the document, the unit catalog, and the metric keys and status codes that the
/// target machine already has.
/// </summary>
public record ExtractionInput
{
    public required IReadOnlyList<ConvertedChunk> Chunks { get; init; }

    public required IReadOnlyList<MetricUnit> Units { get; init; }

    public required IReadOnlyList<string> ExistingMetricKeys { get; init; }

    public required IReadOnlyList<string> ExistingStatusCodes { get; init; }

    /// <summary>Tells whether the document belongs to one machine. A company file belongs to none.</summary>
    public required bool IsMachineFile { get; init; }
}
