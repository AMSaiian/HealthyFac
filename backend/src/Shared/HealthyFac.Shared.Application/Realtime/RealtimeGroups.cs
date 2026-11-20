namespace HealthyFac.Shared.Application.Realtime;

/// <summary>
/// Builds the names of the hub groups. The gateway and every pusher build them here, so both sides agree.
/// </summary>
public static class RealtimeGroups
{
    public static string ForKind(Guid companyId, RealtimeNotificationKind kind)
        => $"company:{companyId:D}:kind:{kind.Value}";

    public static string ForTopic(Guid companyId, RealtimeTopic topic)
        => $"company:{companyId:D}:topic:{topic.Value}";
}
