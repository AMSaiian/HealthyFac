namespace HealthyFac.Shared.Application.Realtime;

public sealed record RealtimeHint
{
    public required RealtimeTopic Topic { get; init; }

    public Guid? CaseId { get; init; }

    public Guid? AttemptId { get; init; }

    public Guid? MachineId { get; init; }

    /// <summary>The work order that changed, for a <c>workOrders</c> hint, so an open view can match it.</summary>
    public Guid? WorkOrderId { get; init; }
}
