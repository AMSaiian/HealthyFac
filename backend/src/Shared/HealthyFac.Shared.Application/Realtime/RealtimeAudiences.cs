namespace HealthyFac.Shared.Application.Realtime;

/// <summary>
/// Maps each group kind and each topic to the permission that a user needs to receive it.
/// </summary>
/// <remarks>
/// A user-targeted kind has no entry. The gateway joins a connection to the group of each entry whose permission the
/// token holds.
/// </remarks>
public static class RealtimeAudiences
{
    public static readonly IReadOnlyDictionary<RealtimeNotificationKind, string> KindPermissions =
        new Dictionary<RealtimeNotificationKind, string>
        {
            [RealtimeNotificationKind.WorkOrderBlocked] = "work-orders:manage",
            [RealtimeNotificationKind.WorkOrderDraftCreated] = "drafts:read",
            [RealtimeNotificationKind.ProvisioningFailed] = "employees:manage",
            [RealtimeNotificationKind.MaintenanceCaseOpened] = "cases:read",
        };

    public static readonly IReadOnlyDictionary<RealtimeTopic, string> TopicPermissions =
        new Dictionary<RealtimeTopic, string>
        {
            [RealtimeTopic.WorkOrders] = "work-orders:read",
            [RealtimeTopic.Schedule] = "shifts:read",
            [RealtimeTopic.Machines] = "machines:read",
            [RealtimeTopic.Cases] = "cases:read",
            [RealtimeTopic.DefinitionDrafts] = "definition-drafts:read",
        };
}
