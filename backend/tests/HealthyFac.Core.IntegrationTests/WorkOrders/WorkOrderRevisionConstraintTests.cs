using EntityFramework.Exceptions.Common;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreWorkOrdersCollection.Name)]
public sealed class WorkOrderRevisionConstraintTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SaveChanges_WithWorkOrderAndFirstRow_CommitsBoth()
    {
        var companyId = await _fixture.CreateCompanyAsync(WorkOrderTestData.CompanyName);
        var workOrder = _fixture.NewWorkOrder(companyId);

        await _fixture.SaveWorkOrderAsync(workOrder);

        var workOrderCount = await _fixture.CountWorkOrdersAsync(workOrder.Id);
        var rowCount = await _fixture.CountHistoryRowsAsync(workOrder.Id);

        workOrderCount.Should().Be(expected: 1);
        rowCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task SaveChanges_WithCurrentRevisionOfMissingRow_FailsAtCommit()
    {
        var companyId = await _fixture.CreateCompanyAsync(WorkOrderTestData.CompanyName);
        var workOrder = _fixture.NewWorkOrder(companyId);

        var act = () => _fixture.SaveWorkOrderAsync(workOrder, WorkOrderTestData.MissingRevision);

        await act.Should().ThrowAsync<DbUpdateException>();
        var workOrderCount = await _fixture.CountWorkOrdersAsync(workOrder.Id);

        workOrderCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task SaveChanges_WithCurrentRevisionOfOtherWorkOrder_FailsAtCommit()
    {
        var employee = await _fixture.ArrangeProvisioningEmployeeAsync(WorkOrderTestData.EmployeeEmail);
        var other = _fixture.NewWorkOrder(employee.CompanyId);
        other.Assign(employee.EmployeeId, DateTime.UnixEpoch, DateTime.UnixEpoch);
        var workOrder = _fixture.NewWorkOrder(employee.CompanyId);

        await _fixture.SaveWorkOrderAsync(other);
        var act = () => _fixture.SaveWorkOrderAsync(workOrder, WorkOrderTestData.SecondRevision);

        await act.Should().ThrowAsync<DbUpdateException>();
        var workOrderCount = await _fixture.CountWorkOrdersAsync(workOrder.Id);

        workOrderCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task SaveChanges_WithSecondRowOfSameRevision_FailsWithUniqueConstraint()
    {
        var companyId = await _fixture.CreateCompanyAsync(WorkOrderTestData.CompanyName);
        var workOrder = _fixture.NewWorkOrder(companyId);
        await _fixture.SaveWorkOrderAsync(workOrder);

        var act = () => _fixture.InsertHistoryRowAsync(workOrder.Id, revision: 1);

        await act.Should().ThrowAsync<UniqueConstraintException>();
        var rowCount = await _fixture.CountHistoryRowsAsync(workOrder.Id);

        rowCount.Should().Be(expected: 1);
    }
}
