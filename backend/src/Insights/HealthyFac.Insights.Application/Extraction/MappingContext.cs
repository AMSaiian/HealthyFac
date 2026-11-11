using HealthyFac.Insights.Application.MetricUnits;

namespace HealthyFac.Insights.Application.Extraction;

/// <summary>
/// What the mapper needs besides the extraction: the identity of the attempt and the file, the target machine, and the
/// unit catalog.
/// </summary>
public record MappingContext
{
    public required Guid AttemptId { get; init; }

    public required Guid FileId { get; init; }

    public required Guid CompanyId { get; init; }

    public required bool IsMachineFile { get; init; }

    /// <summary>The machine of the file. A machine file has one. A company file has none.</summary>
    public Guid? FileMachineId { get; init; }

    public required DateTime QueuedAt { get; init; }

    public required IReadOnlyList<MetricUnit> Units { get; init; }
}
