using HealthyFac.Core.Domain.Drafts;

namespace HealthyFac.Core.Application.Drafts;

/// <summary>
/// A machine draft with all its values.
/// </summary>
public record MachineDraftDetail
{
    public required Guid Id { get; init; }

    public required string State { get; init; }

    public required bool IsCreate { get; init; }

    public required Guid AttemptId { get; init; }

    public required Guid FileId { get; init; }

    public string? Name { get; init; }

    public string? SerialNumber { get; init; }

    public string? Description { get; init; }

    /// <summary>The row version of the draft.</summary>
    public required uint RowVersion { get; init; }

    public static MachineDraftDetail From(MachineDraft draft) =>
        new()
        {
            Id = draft.Id,
            State = draft.State.Value,
            IsCreate = draft.IsCreate,
            AttemptId = draft.AttemptId,
            FileId = draft.FileId,
            Name = draft.Name,
            SerialNumber = draft.SerialNumber,
            Description = draft.Description,
            RowVersion = draft.RowVersion,
        };
}
