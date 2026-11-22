using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.WorkOrders.Data;
using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.IntegrationTests.WorkOrders;

[Collection(SeederCollection.Name)]
public sealed class WorkOrdersSeedTests(SeederFixture fixture)
{
    private readonly SeederFixture _fixture = fixture;

    [Fact]
    public async Task RunAsync_WithTheRealWorkOrdersFile_StoresThe13OrdersWithTheirCurrentStatuses()
    {
        await _fixture.RunFullSeedAsync(TestContext.Current.CancellationToken, WorkOrdersTestData.Anchor);

        var orders = await _fixture.LoadWorkOrdersAsync();
        orders.Should().HaveCount(WorkOrdersTestData.OrderCount);
        orders.GroupBy(order => order.Current.Status)
            .ToDictionary(group => group.Key, group => group.Count())
            .Should()
            .BeEquivalentTo(WorkOrdersTestData.StatusCounts);
    }

    [Fact]
    public async Task GetWorkOrderHistory_OfThePumpSealOrder_ReturnsTheRowsWithTheirMomentsAndActors()
    {
        var context = await _fixture.RunFullSeedAsync(
            TestContext.Current.CancellationToken, WorkOrdersTestData.Anchor);

        var orders = await _fixture.LoadWorkOrdersAsync();
        var pumpSeal = orders.Single(order => order.Title == WorkOrdersTestData.PumpSealTitle);
        var rows = await _fixture.LoadHistoryAsync(context, pumpSeal.Id, TestContext.Current.CancellationToken);
        var history = rows.OrderBy(row => row.Revision).ToList();
        history.Select(row => row.Status).Should().Equal(WorkOrdersTestData.PumpSealStatuses);
        history.Select(row => row.CreatedBy)
            .Should()
            .Equal(WorkOrdersTestData.PumpSealActors.Select(actor => context.GetActor(actor).UserId));
        for (var index = 0; index < history.Count; index++)
        {
            var expected = SeedCalendar.ToUtc(
                RelativeMoment.Parse(WorkOrdersTestData.PumpSealMoments[index]),
                WorkOrdersTestData.Anchor,
                context.Zone);
            history[index].CreatedAt.Should().BeCloseTo(expected.UtcDateTime, TimeSpan.FromSeconds(seconds: 1));
        }
    }

    [Fact]
    public async Task RunAsync_WithAStartOutsideTheShift_StopsWithTheOrderKeyAndOutsideShift()
    {
        var folder = WorkOrderSeedVariants.BearingVibrationOutsideShift();

        var act = () => _fixture.RunFullSeedAsync(
            TestContext.Current.CancellationToken, WorkOrdersTestData.Anchor, folder);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage($"*bearing-vibration*{WorkOrdersTestData.OutsideShiftMessage}*");
    }

    [Fact]
    public async Task RunAsync_WithATakenSlot_StopsWithTheOrderKeyAndSlotTaken()
    {
        var folder = WorkOrderSeedVariants.PumpCouplingSlotTaken();

        var act = () => _fixture.RunFullSeedAsync(
            TestContext.Current.CancellationToken, WorkOrdersTestData.Anchor, folder);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage($"*pump-coupling*{WorkOrdersTestData.SlotTakenMessage}*");
    }

    [Fact]
    public async Task RunAsync_WithAStatusChangeOnABacklogOrder_StopsWithTheOrderKey()
    {
        var folder = WorkOrderSeedVariants.StatusChangeOnBacklogOrder();

        var act = () => _fixture.RunFullSeedAsync(
            TestContext.Current.CancellationToken, WorkOrdersTestData.Anchor, folder);

        await act.Should().ThrowAsync<Exception>().WithMessage(expectedWildcardPattern: "*wear-rings*");
    }

    [Fact]
    public async Task RunAsync_WithThePumpDraftAppliedTwice_StopsWithTheDraftItemAndNotPending()
    {
        var folder = WorkOrderSeedVariants.PumpDraftAppliedTwice();

        var act = () => _fixture.RunFullSeedAsync(
            TestContext.Current.CancellationToken, WorkOrdersTestData.Anchor, folder);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage($"*machines.json#pump-again*{WorkOrdersTestData.DraftNotPendingMessage}*");
    }
}
