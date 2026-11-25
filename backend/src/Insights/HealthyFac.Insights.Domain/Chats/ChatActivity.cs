using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Chats;

/// <summary>
/// Defines the tool groups that a chat snapshot shows while the model reads. The camelCase values reach the web app,
/// so never rename one in place.
/// </summary>
public sealed class ChatActivity : Enumeration<ChatActivity>
{
    public static readonly ChatActivity Machines = new(value: "machines");
    public static readonly ChatActivity Signals = new(value: "signals");
    public static readonly ChatActivity Cases = new(value: "cases");
    public static readonly ChatActivity WorkOrders = new(value: "workOrders");
    public static readonly ChatActivity People = new(value: "people");
    public static readonly ChatActivity Schedule = new(value: "schedule");
    public static readonly ChatActivity Manuals = new(value: "manuals");
    public static readonly ChatActivity Files = new(value: "files");
    public static readonly ChatActivity Drafts = new(value: "drafts");

    private ChatActivity(string value)
        : base(value)
    {
    }
}
