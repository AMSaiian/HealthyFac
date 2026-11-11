using System.Linq.Expressions;
using HealthyFac.Insights.Domain.Ingest;

namespace HealthyFac.Insights.Application.Ingest;

/// <summary>
/// One ingest attempt as the web app reads it.
/// </summary>
public record IngestAttemptResponse
{
    public static readonly Expression<Func<IngestAttempt, IngestAttemptResponse>> Projection =
        attempt => new IngestAttemptResponse
        {
            Id = attempt.Id,
            FileId = attempt.FileId,
            MachineId = attempt.MachineId,
            FileName = attempt.FileName,
            Extension = attempt.Extension,
            SizeBytes = attempt.SizeBytes,
            State = attempt.State,
            ErrorCode = attempt.ErrorCode,
            InterruptReason = attempt.InterruptReason,
            Warning = attempt.Warning,
            QueuedAt = attempt.QueuedAt,
            StartedAt = attempt.StartedAt,
            FinishedAt = attempt.FinishedAt,
            PageCount = attempt.PageCount,
            ChunkCount = attempt.ChunkCount,
            ProposedDraftCount = attempt.ProposedDraftCount,
        };

    public required Guid Id { get; init; }

    public required Guid FileId { get; init; }

    public Guid? MachineId { get; init; }

    public required string FileName { get; init; }

    public required string Extension { get; init; }

    public required long SizeBytes { get; init; }

    public required IngestState State { get; init; }

    public IngestErrorCode? ErrorCode { get; init; }

    public IngestInterruptReason? InterruptReason { get; init; }

    /// <summary>A translation key with values (JSON) that tells the user about a partial result.</summary>
    public string? Warning { get; init; }

    public required DateTime QueuedAt { get; init; }

    public DateTime? StartedAt { get; init; }

    public DateTime? FinishedAt { get; init; }

    public int? PageCount { get; init; }

    public int? ChunkCount { get; init; }

    public int? ProposedDraftCount { get; init; }
}
