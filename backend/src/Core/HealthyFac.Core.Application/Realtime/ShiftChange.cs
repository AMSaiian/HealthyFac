namespace HealthyFac.Core.Application.Realtime;

/// <summary>
/// A shift change with the identity user of its employee. A use case reads the user inside its transaction
/// operation and returns this record, so the code after the commit only adds events.
/// </summary>
public sealed record ShiftChange
{
    public required Guid EmployeeId { get; init; }

    /// <summary>The web user of the employee, or <see langword="null"/> when the employee has none.</summary>
    public Guid? IdentityUserId { get; init; }

    /// <summary>The first local date that the change touches.</summary>
    public required DateOnly Date { get; init; }
}
