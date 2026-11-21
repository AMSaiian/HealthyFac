namespace HealthyFac.Core.Application.Realtime;

public sealed record WorkOrderDraftCreatedData
{
    public required Guid DraftId { get; init; }

    public required string Title { get; init; }
}
