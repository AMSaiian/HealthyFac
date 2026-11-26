using System.Text.Json;
using HealthyFac.Core.Application.Realtime;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Drafts.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Insights.Messaging.WorkOrders;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.IntegrationTests.WorkOrders.Drafts;

[Collection(CoreWorkOrdersCollection.Name)]
[Feature(TestFeatures.WorkOrders)]
[Feature(TestFeatures.Drafts)]
public sealed class WorkOrderDraftRealtimeTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Inbox_WithANewProposal_RecordsDraftCreatedToTheKindGroupAfterTheCommit()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var message = WorkOrderDraftTestData.Proposal(tenant.CompanyId, machineId, Guid.CreateVersion7());
        _fixture.RealtimeQueue.Clear();

        var outcome = await _fixture.ReceiveWorkOrderDraftThroughInboxAsync(message);

        var push = _fixture.RealtimeQueue.Items.Should().ContainSingle().Subject.Push;
        outcome.IsProcessed.Should().BeTrue();

        push.Group.Should()
            .Be(RealtimeGroups.ForKind(tenant.CompanyId, RealtimeNotificationKind.WorkOrderDraftCreated));

        push.Notification!.Data.Should()
            .BeEquivalentTo(new WorkOrderDraftCreatedData { DraftId = message.DraftId, Title = message.Title });
    }

    [Fact]
    public async Task Inbox_WithAUserActor_RecordsDraftCreatedWithoutActorSubject()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var message = WorkOrderDraftTestData.Proposal(tenant.CompanyId, machineId, Guid.CreateVersion7());
        _fixture.RealtimeQueue.Clear();

        var outcome = await _fixture.ReceiveThroughInboxAsync(
            WorkOrderDraftProposed.MessageType,
            JsonSerializer.Serialize(message, MessagingJsonOptions.Instance),
            Guid.CreateVersion7(),
            tenant.CompanyId);

        var push = _fixture.RealtimeQueue.Items.Should().ContainSingle().Subject.Push;
        outcome.IsProcessed.Should().BeTrue();
        push.Notification!.ActorSubject.Should().BeNull();
    }

    [Fact]
    public async Task Inbox_WithARepeatOfTheSameProposal_RecordsNothing()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var message = WorkOrderDraftTestData.Proposal(tenant.CompanyId, machineId, Guid.CreateVersion7());
        await _fixture.ReceiveWorkOrderDraftThroughInboxAsync(message);
        _fixture.RealtimeQueue.Clear();

        var outcome = await _fixture.ReceiveWorkOrderDraftThroughInboxAsync(message);

        outcome.IsProcessed.Should().BeTrue();
        _fixture.RealtimeQueue.Items.Should().BeEmpty();
    }
}
