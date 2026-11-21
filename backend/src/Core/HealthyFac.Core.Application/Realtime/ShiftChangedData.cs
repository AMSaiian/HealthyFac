namespace HealthyFac.Core.Application.Realtime;

public sealed record ShiftChangedData
{
    public required Guid EmployeeId { get; init; }

    public required DateOnly Date { get; init; }
}
