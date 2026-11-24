using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.People.Data;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Shared.Application.Grids;
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
        )
        {
            var page = await fixture.Hosts.Core.SendAsync<GetWorkOrderHistoryRequest, WorkOrderHistoryPageResponse>(
                context.GetActor(person: "nadiia"),
                new GetWorkOrderHistoryRequest
                {
                    CompanyId = PeopleTestData.CompanyId,
                    Id = orderId,
                    Limit = GridValidationConstants.MaxLimit,
                    Scope = AccessScope.Company,
                },
                cancellationToken);

            return page.Items;
        }
    }
}
