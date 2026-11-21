using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Core.IntegrationTests.Shifts;

[Collection(CoreShiftsCollection.Name)]
public sealed class ShiftRealtimeTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(ShiftRealtimeTestData.Changes), MemberType = typeof(ShiftRealtimeTestData))]
    public async Task ShiftChange_WithEachUseCase_RecordsShiftChangedAndTheScheduleHint(
        string caseName,
        ShiftChangeAction change
    )
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        _fixture.RealtimeQueue.Clear();

        var date = await change(_fixture, tenant);

        var pushes = _fixture.RealtimeQueue.Items.Select(item => item.Push).ToList();
        var userPush = pushes.Should().ContainSingle(push => push.Target == RealtimePushTarget.User, caseName).Subject;
        var hint = pushes.Should().ContainSingle(push => push.Target == RealtimePushTarget.Group, caseName).Subject;

        userPush.UserId.Should().Be(tenant.IdentityUserId);
        userPush.Notification!.Kind.Should().Be(RealtimeNotificationKind.ShiftChanged);
        userPush.Notification.Data.Should().BeEquivalentTo(new { tenant.EmployeeId, Date = date });
        hint.Group.Should().Be(RealtimeGroups.ForTopic(tenant.CompanyId, RealtimeTopic.Schedule));
    }

    [Fact]
    public async Task SetOverride_ForEmployeeWithoutIdentityUser_RecordsOnlyTheHint()
    {
        var tenant = await _fixture.ArrangeUnprovisionedShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        _fixture.RealtimeQueue.Clear();

        await _fixture.SetOverrideAsync(tenant, ShiftTestData.Tomorrow, ShiftTestData.Blocks(ShiftTestData.Morning));

        var push = _fixture.RealtimeQueue.Items.Should().ContainSingle().Subject.Push;
        push.Group.Should().Be(RealtimeGroups.ForTopic(tenant.CompanyId, RealtimeTopic.Schedule));
    }

    [Theory]
    [MemberData(nameof(ShiftRealtimeTestData.Rejections), MemberType = typeof(ShiftRealtimeTestData))]
    public async Task ShiftChange_WhenTheChangeIsRejected_RecordsNothing(
        string caseName,
        ShiftRejectedAction change,
        Type expectedException
    )
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        _fixture.RealtimeQueue.Clear();

        var act = () => change(_fixture, tenant);

        var thrown = await act.Should().ThrowAsync<Exception>(caseName);
        thrown.Which.Should().BeOfType(expectedException, caseName);
        _fixture.RealtimeQueue.Items.Should().BeEmpty(caseName);
    }
}
