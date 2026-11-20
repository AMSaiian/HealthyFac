using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Shared.Application.Realtime;

/// <summary>
/// Names a data area that a real-time hint refers to. A hint tells the web app to reload the data of its topic. The
/// value travels over the hub, so treat it as a contract.
/// </summary>
public sealed class RealtimeTopic : Enumeration<RealtimeTopic>
{
    public static readonly RealtimeTopic WorkOrders = new(value: "workOrders");
    public static readonly RealtimeTopic Schedule = new(value: "schedule");
    public static readonly RealtimeTopic Machines = new(value: "machines");
    public static readonly RealtimeTopic Cases = new(value: "cases");
    public static readonly RealtimeTopic DefinitionDrafts = new(value: "definitionDrafts");

    private RealtimeTopic(string value)
        : base(value)
    {
    }
}
