using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Chats.Messages;

/// <summary>
/// Defines what a link of a message points to.
/// The camelCase values reach the API and the database, so never rename one in place.
/// </summary>
public sealed class ChatLinkKind : Enumeration<ChatLinkKind>
{
    public static readonly ChatLinkKind Machine = new(value: "machine");
    public static readonly ChatLinkKind MaintenanceCase = new(value: "maintenanceCase");
    public static readonly ChatLinkKind File = new(value: "file");
    public static readonly ChatLinkKind Employee = new(value: "employee");
    public static readonly ChatLinkKind WorkOrder = new(value: "workOrder");

    private ChatLinkKind(string value)
        : base(value)
    {
    }
}
