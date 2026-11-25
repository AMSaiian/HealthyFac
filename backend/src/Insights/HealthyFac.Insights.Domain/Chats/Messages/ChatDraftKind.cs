using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Chats.Messages;

/// <summary>
/// Defines the kind of a draft that an assistant message proposed.
/// The camelCase values reach the API and the database, so never rename one in place.
/// </summary>
public sealed class ChatDraftKind : Enumeration<ChatDraftKind>
{
    public static readonly ChatDraftKind Machine = new(value: "machine");
    public static readonly ChatDraftKind Metric = new(value: "metric");
    public static readonly ChatDraftKind Status = new(value: "status");
    public static readonly ChatDraftKind OperatingPhase = new(value: "operatingPhase");
    public static readonly ChatDraftKind WorkOrder = new(value: "workOrder");

    private ChatDraftKind(string value)
        : base(value)
    {
    }
}
