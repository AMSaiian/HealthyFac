using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.People.Data;
using HealthyFac.Seeder.Timeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.IntegrationTests.WorkOrders.Data;

public static class WorkOrdersFixtureExtensions
{
    extension(SeederFixture fixture)
    {
        public async Task<List<WorkOrder>> LoadWorkOrdersAsync()
        {
            using var scope = fixture.Hosts.Core.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .WorkOrders
                .Include(order => order.History)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IReadOnlyList<WorkOrderHistoryResponse>> LoadHistoryAsync(
            SeedContext context,
            Guid orderId,
            CancellationToken cancellationToken
        ) =>
            await fixture.Hosts.Core.SendAsync<GetWorkOrderHistoryRequest, IReadOnlyList<WorkOrderHistoryResponse>>(
                context.GetActor(person: "nadiia"),
                new GetWorkOrderHistoryRequest
                {
                    CompanyId = PeopleTestData.CompanyId,
                    Id = orderId,
                    Scope = AccessScope.Company,
                },
                cancellationToken);
    }
}
