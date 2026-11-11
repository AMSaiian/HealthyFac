using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreWorkOrdersCollection.Name)]
public sealed class AssignWorkOrderRaceTests(CoreDatabaseFixture fixture)
{
    private const int TransactionsOfOneWinnerAndOneRepeatedLoser = 3;

    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task AssignWorkOrder_WithTwoParallelAssignsToOverlappingSlots_EndsWithOneSuccessAndOneSlotTaken()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        await _fixture.SeedVersionAsync(
            tenant.EmployeeId,
            ShiftTestData.Today.AddDays(value: -30),
            ShiftTestData.Morning,
            ShiftTestData.Afternoon);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));
        var first = await CreateBacklogAsync(tenant);
        var second = await CreateBacklogAsync(tenant);
        var firstRowVersion = await _fixture.LoadWorkOrderRowVersionAsync(first);
        var secondRowVersion = await _fixture.LoadWorkOrderRowVersionAsync(second);
        var transactionsBefore = _fixture.Transactions.Count;
        using var race = _fixture.SaveGate.StartRace(ShiftTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => AssignAsync(tenant, first, WorkOrderTestData.ScheduledTime, firstRowVersion)),
            race.RunAsync(() => AssignAsync(tenant, second, WorkOrderTestData.OverlappingTime, secondRowVersion)));

        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<UnprocessableException>()
            .Which.Message.Should()
            .Be(WorkOrderErrors.Rejections.SlotTaken);
        race.SaveCounts.Should().Equal(ShiftTestData.UniqueRaceSaveCounts);
        (_fixture.Transactions.Count - transactionsBefore).Should().Be(TransactionsOfOneWinnerAndOneRepeatedLoser);
    }

    private async Task<Guid> CreateBacklogAsync(ShiftTenant tenant)
    {
        var created = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(tenant.CompanyId, AccessScope.Company));

        return created.Id;
    }

    private async Task AssignAsync(ShiftTenant tenant, Guid workOrderId, string time, uint rowVersion)
    {
        using var scope = _fixture.CreateUserScope(tenant.CompanyId);

        await scope.ServiceProvider
            .GetRequiredService<IRequestHandler<AssignWorkOrderRequest>>()
            .HandleAsync(
                new AssignWorkOrderRequest
                {
                    CompanyId = tenant.CompanyId,
                    Id = workOrderId,
                    AssigneeId = tenant.EmployeeId,
                    PlannedStartLocal = ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time),
                    RowVersion = rowVersion,
                });
    }
}
