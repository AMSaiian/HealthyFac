namespace HealthyFac.Insights.Application.Extraction.Mapping;

/// <summary>
/// What the mapper needs besides the extraction: the identity of the attempt and the file, and the target machine.
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
}
