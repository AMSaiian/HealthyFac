using System.Text.Json;
using HealthyFac.Auth.Messaging.Identities;
using HealthyFac.Core.Application.Realtime;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Infrastructure.Messaging;

namespace HealthyFac.Core.IntegrationTests.Employees;

[Collection(CoreEmployeesCollection.Name)]
public sealed class ProvisioningRealtimeTests(CoreDatabaseFixture fixture)
{
    private const string Reason = "the realm refused the user";

    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Inbox_WithProvisioningFailedForAProvisioningEmployee_RecordsFailedToTheKindGroup()
    {
        var tenant = await _fixture.ArrangeUnprovisionedShiftTenantAsync();
        _fixture.RealtimeQueue.Clear();

        var outcome = await ReceiveAsync(tenant);

        var push = _fixture.RealtimeQueue.Items.Should().ContainSingle().Subject.Push;
        outcome.IsProcessed.Should().BeTrue();
        push.Group.Should().Be(RealtimeGroups.ForKind(tenant.CompanyId, RealtimeNotificationKind.ProvisioningFailed));
        push.Notification!.Data.Should()
            .BeEquivalentTo(
                new ProvisioningFailedData
                {
                    EmployeeName = $"{ShiftTestData.EmployeeFirstName} {ShiftTestData.EmployeeLastName}",
                    Reason = Reason,
                });
    }

    [Fact]
    public async Task Inbox_WithProvisioningFailedForAnActiveEmployee_RecordsNothing()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.RealtimeQueue.Clear();

        var outcome = await ReceiveAsync(tenant);

        outcome.IsProcessed.Should().BeTrue();
        _fixture.RealtimeQueue.Items.Should().BeEmpty();
    }

    private async Task<InboxRowOutcome> ReceiveAsync(ShiftTenant tenant)
    {
        var provisioner = KnownActors.ForServiceFlow(ServiceName.Auth, flow: "provisioner", tenant.CompanyId);
        var message = new IdentityProvisioningFailed { EmployeeId = tenant.EmployeeId, Reason = Reason };

        return await _fixture.ReceiveThroughInboxAsync(
            IdentityProvisioningFailed.MessageType,
            JsonSerializer.Serialize(message, MessagingJsonOptions.Instance),
            provisioner.UserId,
            tenant.CompanyId);
    }
}
