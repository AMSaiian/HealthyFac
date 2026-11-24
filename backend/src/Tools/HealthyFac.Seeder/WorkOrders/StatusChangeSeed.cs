using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.WorkOrders;

public sealed class StatusChangeSeed
{
    public required RelativeMoment At { get; init; }

    public required string By { get; init; }

    public required WorkOrderStatus Status { get; init; }
}
