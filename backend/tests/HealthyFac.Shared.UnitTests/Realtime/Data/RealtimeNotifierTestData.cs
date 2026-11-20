using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Infrastructure.Realtime;
using HealthyFac.Testing.Realtime;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Shared.UnitTests.Realtime.Data;

public sealed class FixedActorProvider(ActorContext? actor) : ICurrentUserProvider
{
    private readonly ActorContext? _actor = actor;

    public Guid? TryGetUserId() => _actor?.UserId;

    public Guid GetUserId() => _actor!.UserId;

    public Guid? TryGetCompanyId() => _actor?.CompanyId;

    public Guid GetCompanyId() => _actor!.CompanyId!.Value;

    public ActorContext? TryGetActor() => _actor;

    public ActorContext GetActor() => _actor!;
}

public sealed class NotifierFixture
{
    public NotifierFixture(ActorContext? actor)
    {
        Queue = new RecordingRealtimeQueue();
        Time = new FakeTimeProvider(RealtimeNotifierTestData.Now);
        Notifier = new RealtimeNotifier(new FixedActorProvider(actor), Time, Queue);
    }

    public RecordingRealtimeQueue Queue { get; }

    public FakeTimeProvider Time { get; }

    public IRealtimeNotifier Notifier { get; }
}

public static class RealtimeNotifierTestData
{
    public static readonly DateTimeOffset Now = new(
        year: 2026, month: 11, day: 20, hour: 10, minute: 0, second: 0, TimeSpan.Zero);

    public static readonly Guid CompanyId = Guid.Parse(input: "11111111-1111-1111-1111-111111111111");
    public static readonly Guid ActorUserId = Guid.Parse(input: "22222222-2222-2222-2222-222222222222");
    public static readonly Guid OtherUserId = Guid.Parse(input: "33333333-3333-3333-3333-333333333333");

    public static readonly ActorContext UserActor = new()
    {
        Kind = ActorKind.User,
        UserId = ActorUserId,
        CompanyId = CompanyId,
    };

    public static readonly ActorContext ServiceActor = new()
    {
        Kind = ActorKind.Service,
        UserId = Guid.Parse(input: "44444444-4444-4444-4444-444444444444"),
        Name = "core:outboxProcessor",
    };

    public static TheoryData<RealtimeNotificationKind, string> GroupKinds =>
        new()
        {
            {
                RealtimeNotificationKind.WorkOrderBlocked,
                "company:11111111-1111-1111-1111-111111111111:kind:workOrderBlocked"
            },
            {
                RealtimeNotificationKind.WorkOrderDraftCreated,
                "company:11111111-1111-1111-1111-111111111111:kind:workOrderDraftCreated"
            },
            {
                RealtimeNotificationKind.ProvisioningFailed,
                "company:11111111-1111-1111-1111-111111111111:kind:provisioningFailed"
            },
            {
                RealtimeNotificationKind.MaintenanceCaseOpened,
                "company:11111111-1111-1111-1111-111111111111:kind:maintenanceCaseOpened"
            },
        };
}
