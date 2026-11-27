using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Messaging.WorkOrders;

/// <summary>
/// Lists where the proposal of a <see cref="WorkOrderDraftProposed"/> message came from. The camelCase values reach the
/// message body, so never rename a value in place: stored outbox and inbox rows use it.
/// </summary>
public sealed class WorkOrderProposalSource : Enumeration<WorkOrderProposalSource>
{
    public static readonly WorkOrderProposalSource Case = new(value: "case");
    public static readonly WorkOrderProposalSource Chat = new(value: "chat");

    private WorkOrderProposalSource(string value)
        : base(value)
    {
    }
}
