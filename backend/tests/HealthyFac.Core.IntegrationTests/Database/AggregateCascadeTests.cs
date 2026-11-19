using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Machines;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HealthyFac.Core.IntegrationTests.Database;

[Collection(CoreMachinesCollection.Name)]
public sealed class AggregateCascadeTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task ExecuteDelete_WorkOrderWithFilesAndHistory_RemovesTheChildRows()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var workOrder = _fixture.NewWorkOrder(tenant.CompanyId);
        workOrder.ReplaceFiles([WorkOrderTestData.FileA, WorkOrderTestData.FileB]);
        await _fixture.SaveWorkOrderAsync(workOrder);
        await _fixture.InsertHistoryRowAsync(workOrder.Id, revision: 2);

        await ExecuteDeleteWorkOrderAsync(workOrder.Id);

        var fileRows = await _fixture.CountFileLinkRowsAsync(workOrder.Id);
        var historyRows = await _fixture.CountHistoryRowsAsync(workOrder.Id);
        var workOrders = await _fixture.CountWorkOrdersAsync(workOrder.Id);

        fileRows.Should().Be(expected: 0);
        historyRows.Should().Be(expected: 0);
        workOrders.Should().Be(expected: 0);
    }

    [Fact]
    public async Task ExecuteDelete_MachineWithWorkOrder_FailsOnTheForeignKey()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);
        var workOrder = WorkOrder.Create(
            tenant.CompanyId,
            WorkOrderTestData.Title,
            description: null,
            WorkOrderPriority.Medium,
            WorkOrderCategory.Repair,
            machineId,
            TimeSpan.FromHours(value: 1));
        await _fixture.SaveWorkOrderAsync(workOrder);

        var act = () => ExecuteDeleteMachineAsync(machineId);

        await act.Should()
            .ThrowAsync<PostgresException>()
            .Where(exception => exception.SqlState == PostgresErrorCodes.RestrictViolation);

        var stateHistory = await _fixture.LoadStateHistoryAsync(machineId);
        var machineRows = await _fixture.LoadMachineRowsAsync(tenant.CompanyId, MachineTestData.SerialNumber);
        var workOrders = await _fixture.CountWorkOrdersAsync(workOrder.Id);

        stateHistory.Should().ContainSingle();
        machineRows.Should().ContainSingle();
        workOrders.Should().Be(expected: 1);
    }

    private async Task ExecuteDeleteWorkOrderAsync(Guid workOrderId)
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

        await dbContext.WorkOrders
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(workOrder => workOrder.Id == workOrderId)
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    private async Task ExecuteDeleteMachineAsync(Guid machineId)
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

        await dbContext.Machines
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(machine => machine.Id == machineId)
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }
}
