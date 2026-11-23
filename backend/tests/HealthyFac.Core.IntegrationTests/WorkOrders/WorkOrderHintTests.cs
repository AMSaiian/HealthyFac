using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Drafts.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreWorkOrdersCollection.Name)]
public sealed class WorkOrderHintTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(WorkOrderHintTestData.Changes), MemberType = typeof(WorkOrderHintTestData))]
    public async Task WorkOrderChange_WithEachUseCase_RecordsTheWorkOrdersHint(
        string caseName,
        WorkOrderChangeAction change
    )
    {
        var tenant = await WorkOrderHintTestData.ArrangeTenantAsync(_fixture);
        var act = await change(_fixture, tenant);
        _fixture.RealtimeQueue.Clear();

        await act();

        var groups = _fixture.RealtimeQueue.Items.Select(item => item.Push.Group).ToList();
        groups.Should().Contain(RealtimeGroups.ForTopic(tenant.CompanyId, RealtimeTopic.WorkOrders), caseName);
    }

    [Theory]
    [MemberData(nameof(WorkOrderHintTestData.Changes), MemberType = typeof(WorkOrderHintTestData))]
    public async Task WorkOrderChange_WithEachUseCase_NamesTheWorkOrderInTheHint(
        string caseName,
        WorkOrderChangeAction change
    )
    {
        var tenant = await WorkOrderHintTestData.ArrangeTenantAsync(_fixture);
        var act = await change(_fixture, tenant);
        _fixture.RealtimeQueue.Clear();

        var workOrderId = await act();

        var hintedIds = HintedWorkOrderIds();
        hintedIds.Should().Contain(workOrderId, caseName);
    }

    [Fact]
    public async Task ApplyScheduleChanges_WithTwoChanges_SendsOneHintForEachWorkOrder()
    {
        var tenant = await WorkOrderHintTestData.ArrangeTenantAsync(_fixture);
        var first = await WorkOrderHintTestData.CreateBacklogAsync(_fixture, tenant);
        var second = await WorkOrderHintTestData.CreateBacklogAsync(_fixture, tenant);
        var firstChange = await WorkOrderHintTestData.ApplyChangeAsync(_fixture, tenant, first, time: "09:00");
        var secondChange = await WorkOrderHintTestData.ApplyChangeAsync(_fixture, tenant, second, time: "10:30");
        _fixture.RealtimeQueue.Clear();

        await WorkOrderHintTestData.ApplyAsync(_fixture, tenant, firstChange, secondChange);

        var hintedIds = HintedWorkOrderIds();
        hintedIds.Should().BeEquivalentTo([first, second]);
    }

    [Fact]
    public async Task ChangeStatus_WithALateCompletionThatSlidesAnotherWorkOrder_HintsBothWorkOrders()
    {
        var tenant = await WorkOrderHintTestData.ArrangeTenantAsync(_fixture);
        var completed = await WorkOrderHintTestData.CreateAssignedAsync(_fixture, tenant, time: "09:00");
        var next = await WorkOrderHintTestData.CreateAssignedAsync(_fixture, tenant, time: "10:00");
        await _fixture.ChangeStatusAsync(
            tenant.CompanyId, Guid.CreateVersion7(), completed, WorkOrderStatus.InProgress);
        _fixture.Clock.Freeze(ShiftTestData.NowOn(ShiftTestData.Tomorrow, time: "10:20"));
        _fixture.RealtimeQueue.Clear();

        await _fixture.ChangeStatusAsync(
            tenant.CompanyId, Guid.CreateVersion7(), completed, WorkOrderStatus.Completed);

        var hintedIds = HintedWorkOrderIds();
        hintedIds.Should().BeEquivalentTo([completed, next]);
    }

    private List<Guid> HintedWorkOrderIds() =>
        _fixture.RealtimeQueue.Items
            .Select(item => item.Push.Hint)
            .OfType<RealtimeHint>()
            .Where(hint => hint.Topic == RealtimeTopic.WorkOrders)
            .Select(hint => hint.WorkOrderId!.Value)
            .ToList();

    [Fact]
    public async Task ApplyWorkOrderDraft_WithAPendingDraft_RecordsTheWorkOrdersHint()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var message = WorkOrderDraftTestData.Proposal(tenant.CompanyId, machineId, Guid.CreateVersion7());
        await _fixture.ProposeWorkOrderDraftAsync(tenant.CompanyId, message);
        _fixture.RealtimeQueue.Clear();

        await _fixture.ApplyWorkOrderDraftAsync(WorkOrderDraftTestData.Apply(tenant.CompanyId, message.DraftId));

        var push = _fixture.RealtimeQueue.Items.Should().ContainSingle().Subject.Push;
        push.Group.Should().Be(RealtimeGroups.ForTopic(tenant.CompanyId, RealtimeTopic.WorkOrders));
    }
}
