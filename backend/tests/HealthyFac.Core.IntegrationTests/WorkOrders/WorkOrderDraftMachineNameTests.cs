using HealthyFac.Core.Application.WorkOrders.Drafts;
using HealthyFac.Core.Domain.Drafts.Proposals;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Machines;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreMachinesCollection.Name)]
[Feature(TestFeatures.WorkOrders)]
[Feature(TestFeatures.Drafts)]
public sealed class WorkOrderDraftMachineNameTests(CoreDatabaseFixture fixture)
{
    private const int PageLimit = 25;

    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Search_ForADraftOfAMachine_ReturnsTheNameOfTheMachine()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId, MachineTestData.MachineName, serialNumber: "DN-1");

        var draftId = await _fixture.SaveWorkOrderDraftAsync(tenant.CompanyId, machineId);

        var page = await SearchAsync(tenant.CompanyId);

        var item = page.Items.Single(candidate => candidate.Id == draftId);
        item.MachineName.Should().Be(MachineTestData.MachineName);
        item.IsMachineDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Search_WithAMachineIdSetFilter_ReturnsOnlyTheDraftsOfThatMachine()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId, MachineTestData.MachineName, serialNumber: "DN-4");

        var otherMachineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId, MachineTestData.MachineName, serialNumber: "DN-5");

        var draftId = await _fixture.SaveWorkOrderDraftAsync(tenant.CompanyId, machineId);
        await _fixture.SaveWorkOrderDraftAsync(tenant.CompanyId, otherMachineId);

        var page = await SearchAsync(tenant.CompanyId, new IdSetFilter { Ids = [machineId] });

        page.Items.Select(item => item.Id).Should().Equal(draftId);
    }

    [Fact]
    public async Task Get_ForADraftOfADeletedMachine_KeepsTheNameOfTheMachine()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId, MachineTestData.MachineName, serialNumber: "DN-2");

        var draftId = await _fixture.SaveWorkOrderDraftAsync(tenant.CompanyId, machineId);
        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        using var scope = _fixture.CreateUserScope(tenant.CompanyId);

        var response = await scope.ServiceProvider
            .GetRequiredService<IRequestHandler<GetWorkOrderDraftRequest, WorkOrderDraftResponse>>()
            .HandleAsync(
                new GetWorkOrderDraftRequest { CompanyId = tenant.CompanyId, Id = draftId },
                TestContext.Current.CancellationToken);

        response.MachineName.Should().Be(MachineTestData.MachineName);
        response.IsMachineDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Get_ForACaseDraft_ShowsTheCaseProposal()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId, MachineTestData.MachineName, serialNumber: "DN-3");

        var draftId = await _fixture.SaveWorkOrderDraftAsync(tenant.CompanyId, machineId);

        var proposal = (await GetAsync(tenant.CompanyId, draftId)).Proposal;

        proposal.Source.Should().Be(DraftSource.Case);
        proposal.CaseId.Should().NotBeNull();
        proposal.ChatId.Should().BeNull();
        proposal.AttemptId.Should().BeNull();
    }

    [Fact]
    public async Task Get_ForAChatDraft_ShowsTheChatProposalWithoutACase()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var message = WorkOrderDraftTestData.ChatProposal(tenant.CompanyId, machineId: null, Guid.CreateVersion7());
        await _fixture.ProposeWorkOrderDraftAsync(tenant.CompanyId, message);
        var stored = await _fixture.LoadWorkOrderDraftsAsync();

        var proposal = (await GetAsync(tenant.CompanyId, stored.Single().Id)).Proposal;

        proposal.Source.Should().Be(DraftSource.Chat);
        proposal.ChatId.Should().Be(message.ChatId);
        proposal.CaseId.Should().BeNull();
        proposal.AttemptId.Should().BeNull();
    }

    private async Task<WorkOrderDraftResponse> GetAsync(Guid companyId, Guid draftId)
    {
        using var scope = _fixture.CreateUserScope(companyId);

        return await scope.ServiceProvider
            .GetRequiredService<IRequestHandler<GetWorkOrderDraftRequest, WorkOrderDraftResponse>>()
            .HandleAsync(new GetWorkOrderDraftRequest { CompanyId = companyId, Id = draftId });
    }

    private async Task<GridResponse<WorkOrderDraftResponse>> SearchAsync(Guid companyId, IdSetFilter? machineIds = null)
    {
        using var scope = _fixture.CreateUserScope(companyId);

        return await scope.ServiceProvider
            .GetRequiredService<IRequestHandler<SearchWorkOrderDraftsRequest, GridResponse<WorkOrderDraftResponse>>>()
            .HandleAsync(
                new SearchWorkOrderDraftsRequest
                {
                    CompanyId = companyId,
                    Offset = 0,
                    Limit = PageLimit,
                    Filter = new WorkOrderDraftGridFilter { MachineIds = machineIds },
                });
    }
}
