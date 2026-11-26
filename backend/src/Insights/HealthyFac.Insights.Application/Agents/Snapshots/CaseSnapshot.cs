using HealthyFac.Insights.Domain.Cases;

namespace HealthyFac.Insights.Application.Agents.Snapshots;

/// <summary>
/// The view of a case that an agent reads: the machine, the times of the case, and each signal with its rule,
/// severity, state, subject, snapshot values, times, and coverage. It holds no names and no user IDs.
/// </summary>
public sealed record CaseSnapshot
{
    public required Guid MachineId { get; init; }

    public required DateTime CaseOpenedAt { get; init; }

    public required IReadOnlyList<CaseSnapshotSignal> Signals { get; init; }

    /// <summary>
    /// Builds the snapshot of a case with its signals, in the order of the open time.
    /// </summary>
    /// <param name="maintenanceCase">The case, with its signals loaded.</param>
    /// <returns>The snapshot.</returns>
    public static CaseSnapshot From(MaintenanceCase maintenanceCase) =>
        new()
        {
            MachineId = maintenanceCase.MachineId,
            CaseOpenedAt = maintenanceCase.OpenedAt,
            Signals =
            [
                .. maintenanceCase.Signals
                    .OrderBy(signal => signal.OpenedAt)
                    .ThenBy(signal => signal.Id)
                    .Select(CaseSnapshotSignal.From),
            ],
        };
}
