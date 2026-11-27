using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.Lookups;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreWorkOrdersCollection.Name)]
[Feature(TestFeatures.WorkOrders)]
[Feature(TestFeatures.Chat)]
public sealed class LookupWorkOrdersTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task LookupWorkOrders_InCompanyScope_ReturnsEveryMatchingWorkOrderSortedByTitle()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var supervisorId = Guid.CreateVersion7();
        var second = await CreateAsync(tenant, supervisorId, AccessScope.Company, title: "Lookup b");
        var first = await CreateAsync(tenant, supervisorId, AccessScope.Company, title: "lookup a");
        var workers = await CreateAsync(tenant, tenant.IdentityUserId, AccessScope.Own, title: "Lookup c");
        await CreateAsync(tenant, supervisorId, AccessScope.Company, title: "Other");

        var response = await LookupAsync(tenant, AccessScope.Company, search: "LOOKUP");

        response.Items.Select(item => item.Id).Should().Equal(first, second, workers);
        response.Items.Select(item => item.DisplayName).Should().Equal("lookup a", "Lookup b", "Lookup c");
    }

    [Fact]
    public async Task LookupWorkOrders_InOwnScope_ReturnsOnlyTheOwnWorkOrders()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var supervisorId = Guid.CreateVersion7();
        var own = await CreateAsync(tenant, tenant.IdentityUserId, AccessScope.Own, title: "Lookup own");
        await CreateAsync(tenant, supervisorId, AccessScope.Company, title: "Lookup foreign");

        var response = await LookupAsync(tenant, AccessScope.Own, search: "Lookup");

        response.Items.Select(item => item.Id).Should().Equal(own);
    }

    [Fact]
    public async Task LookupWorkOrders_WithMoreMatchesThanTheCap_ReturnsTheCap()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var supervisorId = Guid.CreateVersion7();

        for (var number = 0; number <= LookupValidationConstants.MaxItems; number++)
        {
            await CreateAsync(tenant, supervisorId, AccessScope.Company, $"Cap {number:D3}");
        }

        var response = await LookupAsync(tenant, AccessScope.Company, search: "Cap ");

        response.Items.Should().HaveCount(LookupValidationConstants.MaxItems);
        response.Items.Select(item => item.DisplayName).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task LookupWorkOrders_AfterTheWorkOrderIsDeleted_LeavesItOut()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var supervisorId = Guid.CreateVersion7();
        var kept = await CreateAsync(tenant, supervisorId, AccessScope.Company, title: "Lookup kept");
        var removed = await CreateAsync(tenant, supervisorId, AccessScope.Company, title: "Lookup removed");
        await DeleteAsync(tenant, removed);

        var response = await LookupAsync(tenant, AccessScope.Company, search: "Lookup");

        response.Items.Select(item => item.Id).Should().Equal(kept);
    }

    private async Task<Guid> CreateAsync(ShiftTenant tenant, Guid creatorId, AccessScope scope, string title)
    {
        var created = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            creatorId,
            WorkOrderTestData.NewRequest(tenant.CompanyId, scope, title: title));

        return created.Id;
    }

    private async Task DeleteAsync(ShiftTenant tenant, Guid workOrderId)
    {
        var rowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);
        using var scope = _fixture.CreateUserScope(tenant.CompanyId);

        await scope.ServiceProvider
            .GetRequiredService<IRequestHandler<DeleteWorkOrderRequest>>()
            .HandleAsync(
                new DeleteWorkOrderRequest { CompanyId = tenant.CompanyId, Id = workOrderId, RowVersion = rowVersion });
    }

    private Task<LookupResponse> LookupAsync(ShiftTenant tenant, AccessScope scope, string search) =>
        _fixture.ReadAsAsync<LookupWorkOrdersRequest, LookupResponse>(
            tenant.CompanyId,
            tenant.IdentityUserId,
            new LookupWorkOrdersRequest { CompanyId = tenant.CompanyId, Scope = scope, Search = search });
}
