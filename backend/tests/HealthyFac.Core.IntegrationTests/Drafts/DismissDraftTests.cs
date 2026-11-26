using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Drafts.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Machines;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Drafts;

[Collection(CoreMachinesCollection.Name)]
public sealed class DismissDraftTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task DismissDraft_OfACreateDraft_SendsOneMachineDraftDismissed()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var draftId = await _fixture.AddMachineDraftAsync(tenant.CompanyId);

        await _fixture.DismissDraftAsync(tenant.CompanyId, draftId);

        var messages = await _fixture.ReadOutboxMessagesAsync<MachineDraftDismissed>();

        messages.Should().ContainSingle().Which.DraftId.Should().Be(draftId);
        messages[0].CompanyId.Should().Be(tenant.CompanyId);
    }

    [Fact]
    public async Task DismissDraft_OfAnUpdateDraft_SendsNoMessage()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var draftId = await _fixture.AddMachineDraftAsync(tenant.CompanyId, machineId);

        await _fixture.DismissDraftAsync(tenant.CompanyId, draftId);

        var messages = await _fixture.ReadOutboxMessagesAsync<MachineDraftDismissed>();

        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task DismissDraft_WithAnUnknownId_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var act = () => _fixture.DismissDraftAsync(tenant.CompanyId, Guid.CreateVersion7());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
