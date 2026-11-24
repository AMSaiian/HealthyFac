using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.WorkOrders;

public sealed class AssignmentSeed
{
    public required RelativeMoment At { get; init; }

    public required string By { get; init; }

    public required string Assignee { get; init; }

    /// <summary>The planned start. The moment holds the local day and the local time.</summary>
    public required RelativeMoment PlannedStart { get; init; }
}
