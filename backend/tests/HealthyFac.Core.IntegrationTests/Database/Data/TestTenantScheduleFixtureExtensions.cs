using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Database.Data;

public static class TestTenantScheduleFixtureExtensions
{
    extension(CoreDatabaseFixture fixture)
    {
        public async Task<IReadOnlyList<SeededWorkOrder>> LoadSeededWorkOrdersAsync(Guid companyId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

            return await dbContext.WorkOrders
                .AsNoTracking()
                .Where(order => order.CompanyId == companyId)
                .Join(
                    dbContext.WorkOrderHistories,
                    order => new { order.Id, Revision = order.CurrentRevision },
                    row => new { Id = row.WorkOrderId, row.Revision },
                    (order, row) => new SeededWorkOrder
                    {
                        Status = row.Status,
                        AssigneeId = row.AssigneeId,
                        PlannedStartLocal = row.PlannedStartLocal,
                        Estimate = order.Estimate,
                    })
                .ToListAsync();
        }

        public async Task<IReadOnlyList<SeededShiftBlock>> LoadSeededShiftBlocksAsync(Guid companyId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

            return await dbContext.ShiftTemplateBlocks
                .AsNoTracking()
                .Join(
                    dbContext.ShiftTemplateVersions,
                    block => block.ShiftTemplateVersionId,
                    version => version.Id,
                    (block, version) => new { block, version })
                .Join(
                    dbContext.Employees.Where(employee => employee.CompanyId == companyId),
                    pair => pair.version.EmployeeId,
                    employee => employee.Id,
                    (pair, employee) => new SeededShiftBlock
                    {
                        EmployeeId = employee.Id,
                        Weekday = pair.block.Weekday,
                        Start = pair.block.Start,
                        End = pair.block.End,
                    })
                .ToListAsync();
        }
    }
}

public sealed record SeededWorkOrder
{
    public required WorkOrderStatus Status { get; init; }
    public required Guid? AssigneeId { get; init; }
    public required DateTime? PlannedStartLocal { get; init; }
    public required TimeSpan Estimate { get; init; }
}

public sealed record SeededShiftBlock
{
    public required Guid EmployeeId { get; init; }
    public required Weekday Weekday { get; init; }
    public required TimeOnly Start { get; init; }
    public required TimeOnly End { get; init; }
}
