namespace HealthyFac.Shared.Application.Realtime;

public sealed record RealtimeHint
{
    public required RealtimeTopic Topic { get; init; }

    public Guid? CaseId { get; init; }

    public Guid? AttemptId { get; init; }

    public Guid? MachineId { get; init; }
}
